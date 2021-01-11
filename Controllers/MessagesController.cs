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
            //check if userId is equal to tokens nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get message from repo
            var messageFromRepo = await repo.GetMessage(id);

            //if there is no message return nothing
            if (messageFromRepo == null)
                return NotFound();

            //if there is message return it
            return Ok(messageFromRepo);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            //check if userId is equal to tokens nameid
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
            //check if userId is equal to tokens nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //set userid from params to userid
            messageParams.UserId = userId;

            //get messages from db with message parameters
            var messagesFromRepo = await repo.GetMessagesForUser(messageParams);

            //map database's messages to ienumerable of dtos
            var messages = mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            //add pagination to response
            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize,
                messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            //return messages
            return Ok(messages);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            //get sender with userid
            var sender = await repo.GetUser(userId);

            //check if userId is equal to tokens nameid
            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //set message's ender id to userid
            messageForCreationDto.SenderId = userId;

            //get recipientid
            var recipient = await repo.GetUser(messageForCreationDto.RecipientId);

            //if there is no recipient return nothing
            if (recipient == null)
                return BadRequest("მომხმარებელი ვერ ვიპოვეთ");

            //map from dto to class
            var message = mapper.Map<Message>(messageForCreationDto);

            //add message to db
            repo.Add(message);

            //try saving changes
            if (await repo.SaveAll())
            {
                //map from class to dto
                var messageToReturn = mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {userId, id = message.Id }, messageToReturn);
            }

            //throw exception if error
            throw new Exception("მესიჯი ვერ შეინახა და ვერ შეიქმნა");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            //check if userId is equal to tokens nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get message from db
            var messageFromRepo = await repo.GetMessage(id);

            //if sender deleted message
            if (messageFromRepo.SenderId == userId)
                messageFromRepo.SenderDeleted = true;

            //if recipient deleted message
            if (messageFromRepo.RecipientId == userId)
                messageFromRepo.RecipientDeleted = true;

            //if both deleted message delete it from db
            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                repo.Delete(messageFromRepo);

            //save changes and return no content
            if (await repo.SaveAll())
                return NoContent();

            throw new Exception("შეცდომა მესიჯის წაშლის დროს");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            //check if userId is equal to tokens nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get message with id
            var message = await repo.GetMessage(id);

            //if recipient id is not equal to userId
            if (message.RecipientId != userId)
                return Unauthorized();

            //mark as read
            message.IsRead = true;
            //mark when it was read
            message.DateRead = DateTime.Now;

            //savechanges
            await repo.SaveAll();

            //return nocontent
            return NoContent();
        }
    }
}
