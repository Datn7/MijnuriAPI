using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MijnuriAPI.Dtos;
using MijnuriAPI.Interfaces;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MijnuriAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository authRepo;
        private readonly IConfiguration configuration;
        private readonly IMapper mapper;

        public AuthController(IAuthRepository authRepo, IConfiguration configuration, IMapper mapper)
        {
            this.authRepo = authRepo;
            this.configuration = configuration;
            this.mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            //convert username to lowerletters
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            //check if user exists
            if (await authRepo.UserExists(userForRegisterDto.Username))
                return BadRequest("მომხმარებელი უკვე არსებობს");

            //map from userforregister to User class
            var userToCreate = mapper.Map<User>(userForRegisterDto);

            //register user with repo
            var createdUser = await authRepo.Register(userToCreate, userForRegisterDto.Password);

            //map from createduser to usertoreturn
            var userToReturn = mapper.Map<UserForDetailedDto>(createdUser);

            //return StatusCode(201);
            return CreatedAtRoute("GetUser", new { controller = "Users", id = createdUser.Id }, userToReturn);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            //get user from repo and convert username to lowercase both are strings
            var userFromRepo = await authRepo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            //if there is no user return unauthorized
            if (userFromRepo == null)
                return Unauthorized();

            //create claims that id and usernames are there
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)
            };

            //get key from appsettings, its value
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("AppSettings:Token").Value));

            //encrypt gotten key with sha512
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            //return payload nameid unique_name and expire and valid and issued
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            //create token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            //create token from descriptor
            var token = tokenHandler.CreateToken(tokenDescriptor);

            //map from repo to usertolistdto
            var user = mapper.Map<UserForListDto>(userFromRepo);

            //return token and user
            return Ok(new 
            { 
                token = tokenHandler.WriteToken(token),
                user
            });
        }

    }
}
