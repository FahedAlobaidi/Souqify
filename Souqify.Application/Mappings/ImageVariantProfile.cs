using AutoMapper;
using Souqify.Application.DTOs.Image;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Mappings
{
    public class ImageVariantProfile:Profile
    {
        public ImageVariantProfile()
        {
            CreateMap<CreateImageDto, ProductImage>();
        }
    }
}
