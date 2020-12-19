using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MijnuriAPI.Dtos;
using MijnuriAPI.Helpers;
using MijnuriAPI.Interfaces;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MijnuriAPI.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    //[Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository repo;
        private readonly IMapper mapper;

        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            this.repo = repo;
            this.mapper = mapper;
        }

        [HttpGet("{id}", Name ="GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await repo.GetMessage(id);

            if (messageFromRepo == null)
                return NotFound();

            return Ok(messageFromRepo);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messagesFromRepo = await repo.GetMessageThread(userId, recipientId);

            var messageThread = mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            return Ok(messageThread);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId,
           [FromQuery] MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            messageParams.UserId = userId;

            var messagesFromRepo = await repo.GetMessagesForUser(messageParams);

            var messages = mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize,
                messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(messages);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            var sender = await repo.GetUser(userId);

            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            messageForCreationDto.SenderId = userId;

            var recipient = await repo.GetUser(messageForCreationDto.RecipientId);

            if (recipient == null)
                return BadRequest("მომხმარებელი ვერ ვიპოვეთ");

            //map from dto to class
            var message = mapper.Map<Message>(messageForCreationDto);

            repo.Add(message);

            if (await repo.SaveAll())
            {
                var messageToReturn = mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {userId, id = message.Id }, messageToReturn);
            }

            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await repo.GetMessage(id);

            if (messageFromRepo.SenderId == userId)
                messageFromRepo.SenderDeleted = true;

            if (messageFromRepo.RecipientId == userId)
                messageFromRepo.RecipientDeleted = true;

            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                repo.Delete(messageFromRepo);

            if (await repo.SaveAll())
                return NoContent();

            throw new Exception("შეცდომა მესიჯის წაშლის დროს");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var message = await repo.GetMessage(id);

            if (message.RecipientId != userId)
                return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await repo.SaveAll();

            return NoContent();
        }
    }
}
