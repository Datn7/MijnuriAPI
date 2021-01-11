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
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository datingRepo;
        private readonly IMapper mapper;

        public UsersController(IDatingRepository datingRepo, IMapper mapper)
        {
            this.datingRepo = datingRepo;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            //get logged in user's id from token 
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            //get current user with the id we got from token
            var userFromRepo = await datingRepo.GetUser(currentUserId);

            //set id in params
            userParams.UserId = currentUserId;

            //check users gender 
            if (string.IsNullOrEmpty(userParams.Gender))
            {
                //see if its male or female
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }
              
            //pass userparams and get users
            var users = await datingRepo.GetUsers(userParams);

            //map from users to userforlistdto
            var usersToReturn = mapper.Map<IEnumerable<UserForListDto>>(users);

            //add pagination header to the response
            Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            //return automapped mapped users 
            return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name ="GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            //get requested user's id
            var user = await datingRepo.GetUser(id);

            //map from user class to dto
            var userToReturn = mapper.Map<UserForDetailedDto>(user);

            //return user
            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            //get id from logged in users token
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get user from db
            var userFromRepo = await datingRepo.GetUser(id);

            //mao map from dto to repo
            mapper.Map(userForUpdateDto, userFromRepo);

            //if db saved return no content
            if (await datingRepo.SaveAll())
                return NoContent();

            //if there was error during save throw error
            throw new Exception($"მომხმარებლი {id} ვერ შეინახა");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var like = await datingRepo.GetLike(id, recipientId);

            if (like != null)
                return BadRequest("უკვე მოგწონს ესჩემისა");

            if (await datingRepo.GetUser(recipientId) == null)
                return NotFound();

            like = new Like
            {
                LikerId = id,
                LikeeId = recipientId
            };

            datingRepo.Add(like);

            if (await datingRepo.SaveAll())
                return Ok();

            return BadRequest("ვერ მოხერხდა მოწონება");
        }
    }
}
