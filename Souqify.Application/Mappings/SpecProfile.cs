

using AutoMapper;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Specs;
using Souqify.Domain.Entities;

namespace Souqify.Application.Mappings
{
    public class SpecProfile: Profile
    {
        public SpecProfile()
        {
            CreateMap<CreateSpecAttributeDto, SpecAttribute>();
            CreateMap<SpecAttribute, SpecAttributeDto>();
            CreateMap<SpecOption, SpecOptionDto>();
            CreateMap<CreateCategorySpecDto, CategorySpec>();

            // Key, Name, DataType and Unit live on the question (cs.SpecAttribute), not on the form row,
            // so AutoMapper can't find them by name. The SpecAttribute must be loaded (Include) before mapping.
            CreateMap<CategorySpec, CategorySpecDto>()
                .ForMember(d => d.Key, o => o.MapFrom(s => s.SpecAttribute!.Key))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.SpecAttribute!.Name))
                .ForMember(d => d.DataType, o => o.MapFrom(s => s.SpecAttribute!.DataType))
                .ForMember(d => d.Unit, o => o.MapFrom(s => s.SpecAttribute!.Unit));
        }
    }
}
