using AutoMapper;
using GPnaviServer.Dtos;
using GPnaviServer.Models;

namespace GPnaviServer.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserMaster, UserDto>();
            CreateMap<UserDto, UserMaster>();

            CreateMap<UserCsvRow, UserMaster>();

            CreateMap<WSCsvRow, WorkScheduleMaster>();            
        }
    }
}
