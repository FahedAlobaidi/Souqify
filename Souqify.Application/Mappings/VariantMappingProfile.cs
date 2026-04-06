using AutoMapper;
using Souqify.Application.DTOs.Variant;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
