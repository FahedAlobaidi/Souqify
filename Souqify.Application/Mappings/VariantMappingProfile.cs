using AutoMapper;
using Souqify.Application.DTOs.Variant;
using Souqify.Domain;

namespace Souqify.Application.Mappings
{
    public class VariantMappingProfile:Profile
    {
        public VariantMappingProfile()
        {
            CreateMap<CreateVariantDto, ProductVariant>();
            CreateMap<UpdateVariantDto, ProductVariant>();
        }
    }
}
