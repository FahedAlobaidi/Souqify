

using AutoMapper;
using Souqify.Application.DTOs.Category;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using System.Text.RegularExpressions;

namespace Souqify.Application.Services
{
    public class CategoryService:ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoryQueries _categoryQueries;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository,ICategoryQueries categoryQueries,IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _categoryQueries = categoryQueries;
            _mapper = mapper;
        }

        public async Task<CategoryDto> AddCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            if (createCategoryDto == null)
            {
                throw new BadRequestException("Category fields must not be empty");
            }

            //only if there a parent call this repo method
            if(createCategoryDto.ParentId is Guid parentId)
            {
                var parentCategory=await _categoryRepository.IsCategoryExistOrHasProductsAsync(parentId)
                    ?? throw new BadRequestException("Parent category not found");

                if(parentCategory)
                    throw new BadRequestException("The parent already holds products.");
            }

            if(await _categoryRepository.IsCategoryNameUnique(createCategoryDto.Name,createCategoryDto.ParentId))
            {
                throw new BadRequestException("Category with this name is already exist");
            }
            
            if(createCategoryDto.Slug == null)
            {
                var name = Regex.Replace(createCategoryDto.Name.ToLowerInvariant(), "[^a-z0-9]+","-");

                createCategoryDto.Slug = name.Trim('-');
            }

            createCategoryDto.Description ??= string.Empty;

            if (await _categoryRepository.IsCategorySlugExist(createCategoryDto.Slug))
            {
                throw new BadRequestException("Category with this slug is already exist");
            }

            var categoryEnt = _mapper.Map<Category>(createCategoryDto);

            categoryEnt.Id = Guid.NewGuid();

            _categoryRepository.AddCategoryAsync(categoryEnt);

            await _categoryRepository.SaveChangesAsync();

            _categoryQueries.RemoveCategoriesCache();

            return await _categoryQueries.GetCategoryByIdAsync(categoryEnt.Id)?? throw new BadRequestException("Failed to retrieve category after creation");
        }
    }
}
