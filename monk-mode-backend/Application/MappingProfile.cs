﻿using AutoMapper;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;

namespace monk_mode_backend.Application {
    public class MappingProfile : Profile {
        public MappingProfile() {
            CreateMap<TimeBlock, TimeBlockDTO>().ReverseMap()
                .ForMember(dest => dest.Tasks, opt => opt.Ignore())
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.Date, DateTimeKind.Utc)));

            CreateMap<Friendship, FriendshipDTO>();
            CreateMap<UserTask, TaskDTO>().ReverseMap();
            CreateMap<UserTask, CreateTaskDTO>().ReverseMap();
            CreateMap<UserTask, UpdateTaskDTO>().ReverseMap();
        }
    }
}