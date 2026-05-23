

using AutoMapper;
using Souqify.Application.DTOs.Category;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;

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

            if(await _categoryRepository.IsCategoryNameUnique(createCategoryDto.Name))
            {
                throw new BadRequestException("Category with this name is already exist");
            }

            var categoryEnt = _mapper.Map<Category>(createCategoryDto);

            categoryEnt.Id = Guid.NewGuid();

            _categoryRepository.AddCategoryAsync(categoryEnt);

            await _categoryRepository.SaveChangesAsync();

            return await _categoryQueries.GetCategoryByIdAsync(categoryEnt.Id)?? throw new BadRequestException("Failed to retrieve category after creation");
        }
    }
}
