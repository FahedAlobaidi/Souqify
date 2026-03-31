using AutoMapper;
using Souqify.Application.DTOs.Product;
using Souqify.Domain;

namespace Souqify.Application.Mappings
{
    public class ProductMappingProfile:Profile
    {
        public ProductMappingProfile()
        {
            //CreateMap<Product, ProductListDto>()
            //    .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.ProductImages.FirstOrDefault(img => img.IsMain).ImageUrl ?? string.Empty))
            //    .ForMember(dest => dest.InStock, opt => opt.MapFrom(src => src.Variants.Any(v => v.StockQuantity > 0 && v.IsActive)))
            //    .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));
        }
    }
}
