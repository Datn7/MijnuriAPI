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
            var photoFromRepo = await repo.GetPhoto(id);

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
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await repo.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            var currentMainPhoto = await repo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if (await repo.SaveAll())
                return NoContent();

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await repo.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var result = cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    repo.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId == null)
            {
                repo.Delete(photoFromRepo);
            }

            if (await repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }


    }
}
