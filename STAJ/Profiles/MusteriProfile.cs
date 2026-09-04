using AutoMapper;
using STAJ.DTOs;
using STAJ.Entities;

namespace STAJ.Profiles
{
    public class MusteriProfile : Profile
    {
        public MusteriProfile()
        {
            CreateMap<Musteri, MusteriDto>();
        }
    }
}
