using AutoMapper;
using MijnuriAPI.Dtos;
using MijnuriAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            //source to dest
            CreateMap<User, UserForListDto>()
                .ForMember(d => d.PhotoUrl, opt => opt.MapFrom(s => s.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(d => d.Age, opt => opt.MapFrom(s => s.DateOfBirth.CalculateAge()));

            CreateMap<User, UserForDetailedDto>()
                .ForMember(d => d.PhotoUrl, opt => opt.MapFrom(s => s.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(d => d.Age, opt => opt.MapFrom(s => s.DateOfBirth.CalculateAge()));
            CreateMap<Photo, PhotosForDetailedDto>();
            CreateMap<UserForUpdateDto, User>();
            CreateMap<Photo, PhotoForReturnDto>();
            CreateMap<PhotoForCreationDto, Photo>();
        }
    }
}
