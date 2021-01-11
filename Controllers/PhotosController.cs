using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository repo;
        private readonly IMapper mapper;
        private readonly IOptions<CloudinarySettings> cloudinaryConfig;
        private Cloudinary cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this.repo = repo;
            this.mapper = mapper;
            this.cloudinaryConfig = cloudinaryConfig;

            //configure cloudinary config
            Account acc = new Account(
                cloudinaryConfig.Value.CloudName,
                cloudinaryConfig.Value.ApiKey,
                cloudinaryConfig.Value.ApiSecret
            );

            //pass account configuration to a field type of Cloudinary
            cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name ="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            //get photo with photo id
            var photoFromRepo = await repo.GetPhoto(id);

            //map photo to dto
            var photo = mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        //userid comes from query params and dto for mapping
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            //check userid from route is same as token
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get user from repo
            var userFromRepo = await repo.GetUser(userId);

            //create file variable to target dtos file
            var file = photoForCreationDto.File;

            //result from cloudinary store in variable
            var uploadResult = new ImageUploadResult();


            //check if something is in file
            if (file.Length > 0)
            {
                //open stream for file to store inside memory
                using(var stream = file.OpenReadStream())
                {
                    //upload parameters for cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        //pass picture name and stream of photo in memory
                        File = new FileDescription(file.Name, stream),
                        //transform photo to 500x500 and crop to face
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    //get results and store in variable
                    uploadResult = cloudinary.Upload(uploadParams);
                }
            }

            //assign upload result variables to dto
            photoForCreationDto.Url = uploadResult.Url.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;


            //add mapping from dto to class
            var photo = mapper.Map<Photo>(photoForCreationDto);

            //check if picture is main
            if (!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;

            //add photo
            userFromRepo.Photos.Add(photo);

            
            //if it goes right
            if(await repo.SaveAll())
            {
                var photoToReturn = mapper.Map<PhotoForReturnDto>(photo);
                //return CreatedAtRoute("GetPhoto", new { id = photo.Id }, photoToReturn);
                return Ok(photoToReturn);
            }

            //if it goes wrong
            return BadRequest("ვერ დაემატა ფოტო");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            //check if userid is equal to token nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get use with userId
            var user = await repo.GetUser(userId);

            //check if there is photo with photoid
            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            //get photo from db
            var photoFromRepo = await repo.GetPhoto(id);

            //check if photo is main
            if (photoFromRepo.IsMain)
                return BadRequest("ეს უკვე არის თქვენი მთავარი ფოტო");

            //get current main photo from db with userId
            var currentMainPhoto = await repo.GetMainPhotoForUser(userId);

            //set to false if its not main photo
            currentMainPhoto.IsMain = false;

            //set photo to main photo
            photoFromRepo.IsMain = true;

            //save changes and return no content
            if (await repo.SaveAll())
                return NoContent();

            //if things go bad return badrequest
            return BadRequest("მთავარი ფოტო ვერ დაყენდა");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            //check if userId is same as tokens nameid
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            //get userId from repo
            var user = await repo.GetUser(userId);

            //check if passed id is equal to any of the db photos id
            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            //get photo from repo with id passed in queryparams
            var photoFromRepo = await repo.GetPhoto(id);

            //check if photo is main or not
            if (photoFromRepo.IsMain)
                return BadRequest("მთავარ ფოტოს ვერ წაშლით!");

            //if photo that we got from db's public id is not null
            if (photoFromRepo.PublicId != null)
            {
                //initialize cloudinary's deletion params and pass publicId
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                //delete picture on cloudinary
                var result = cloudinary.Destroy(deleteParams);

                //if deleted on cloudinary
                if (result.Result == "ok")
                {
                    //delete from db
                    repo.Delete(photoFromRepo);
                }
            }

            //if we dont have publicId
            if (photoFromRepo.PublicId == null)
            {
                //delete it from database
                repo.Delete(photoFromRepo);
            }

            //save result
            if (await repo.SaveAll())
                return Ok();

            //in any other case return bad request
            return BadRequest("ფოტო ვერ წაიშალა");
        }


    }
}
