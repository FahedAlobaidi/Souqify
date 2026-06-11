using AutoMapper;
using Souqify.Application.DTOs.Category;
using Souqify.Domain.Entities;


namespace Souqify.Application.Mappings
{
    public class CategoryMappingProfile:Profile
    {
        public CategoryMappingProfile()
        {
            //CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>();
        }
    }
}
