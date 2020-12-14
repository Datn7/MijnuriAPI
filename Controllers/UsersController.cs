using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MijnuriAPI.Dtos;
using MijnuriAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Controllers
{
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
        public async Task<IActionResult> GetUsers()
        {
            var users = await datingRepo.GetUsers();

            var usersToReturn = mapper.Map<IEnumerable<UserForListDto>>(users);

            return Ok(usersToReturn);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await datingRepo.GetUser(id);

            var userToReturn = mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);
        }
    }
}
