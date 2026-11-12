
using AutoMapper;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Specs;
using Souqify.Application.Exceptions;
using Souqify.Application.Helpers;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.Services
{
    public class SpecService : ISpecService
    {
        private readonly IMapper _mapper;
        private readonly ISpecRepository _specRepository;
        private readonly ISpecQueries _specQueries;
        private readonly ICategoryRepository _categoryRepository;

        public SpecService(IMapper mapper, ISpecRepository specRepository, ISpecQueries specQueries, ICategoryRepository categoryRepository)
        {
            _mapper = mapper;
            _specRepository = specRepository;
            _specQueries = specQueries;
            _categoryRepository = categoryRepository;
        }

        public async Task<SpecAttributeDto> CreateSpecAttributeAsync(CreateSpecAttributeDto createSpecAttributeDto)
        {
            if (createSpecAttributeDto == null)
                throw new BadRequestException("Please fill the specs");

            if (await _specRepository.IsKeyExistAsync(createSpecAttributeDto.Key))
                throw new BadRequestException($"The key '{createSpecAttributeDto.Key}' is already taken");

            var specAttributeEnt=_mapper.Map<SpecAttribute>(createSpecAttributeDto);

            // No spaces around the name, and a blank unit means "no unit" (NULL), not an empty string.
            specAttributeEnt.Name = specAttributeEnt.Name.Trim();
            specAttributeEnt.Unit = string.IsNullOrWhiteSpace(specAttributeEnt.Unit) ? null : specAttributeEnt.Unit.Trim();

            await _specRepository.CreateSpecAttributeAsync(specAttributeEnt);

            await _specRepository.SaveChangesAsync();

            return _mapper.Map<SpecAttributeDto>(specAttributeEnt);
        }

        public async Task<CategorySpecDto> CreateCategorySpecAsync(Guid categoryId,CreateCategorySpecDto createCategorySpecDto)
        {
            var categoryHasChildren= await _categoryRepository.CategoryHasChildrenAsync(categoryId);

            if (categoryHasChildren == null)
                throw new NotFoundException("Category not found");

            if (categoryHasChildren == true)
                throw new BadRequestException("This category has subcategories. Only a last-level category can have a spec form");

            var specAttributeEnt=await _specRepository.GetSpecAttributeByIdAsync(createCategorySpecDto.SpecAttributeId);

            if(specAttributeEnt == null)
                throw new BadRequestException("Spec not found");

            // To change a row that's already on the form, the admin uses PUT /categories/{categoryId}/specs/{specId}.
            if (await _specRepository.IsCategorySpecExistAsync(categoryId, createCategorySpecDto.SpecAttributeId))
                throw new BadRequestException($"'{specAttributeEnt.Key}' is already on this category's form");

            if (specAttributeEnt.DataType == SpecDataType.Text && createCategorySpecDto.IsFilterable == true)
                throw new BadRequestException($"'{specAttributeEnt.Key}' is a Text spec, so it can't be filterable");

            var categorySpecEnt = _mapper.Map<CategorySpec>(createCategorySpecDto);

            categorySpecEnt.CategoryId = categoryId;

            await _specRepository.CreateCategorySpecAsync(categorySpecEnt);

            await _specRepository.SaveChangesAsync();

            var categorySpecDto = new CategorySpecDto
            {
                SpecAttributeId = createCategorySpecDto.SpecAttributeId,
                DisplayOrder = createCategorySpecDto.DisplayOrder,
                IsFilterable = createCategorySpecDto.IsFilterable,
                IsRequired = createCategorySpecDto.IsRequired,
                DataType = specAttributeEnt.DataType,
                Name = specAttributeEnt.Name,
                Unit = specAttributeEnt.Unit,
                Key = specAttributeEnt.Key,
            };

            return categorySpecDto;
        }

        public async Task<CategorySpecDto> UpdateCategorySpecAsync(Guid categoryId, Guid specId, UpdateCategorySpecDto updateCategorySpecDto)
        {
            // Both ids are in the URL, so a row that isn't there is 404.
            // No separate category checks: if the row exists, the category exists.
            var categorySpecEnt = await _specRepository.GetCategorySpecAsync(categoryId, specId);

            if (categorySpecEnt == null)
                throw new NotFoundException("This spec is not on this category's form");

            var spec = categorySpecEnt.SpecAttribute!;     // loaded by the Include in the repository

            if (spec.DataType == SpecDataType.Text && updateCategorySpecDto.IsFilterable == true)
                throw new BadRequestException($"'{spec.Key}' is a Text spec, so it can't be filterable");

            // The row is tracked, so changing its fields is enough. SaveChanges writes an UPDATE.
            categorySpecEnt.IsRequired = updateCategorySpecDto.IsRequired;
            categorySpecEnt.IsFilterable = updateCategorySpecDto.IsFilterable;
            categorySpecEnt.DisplayOrder = updateCategorySpecDto.DisplayOrder;

            await _specRepository.SaveChangesAsync();

            var categorySpecDto = new CategorySpecDto
            {
                SpecAttributeId = categorySpecEnt.SpecAttributeId,
                DisplayOrder = categorySpecEnt.DisplayOrder,
                IsFilterable = categorySpecEnt.IsFilterable,
                IsRequired = categorySpecEnt.IsRequired,
                DataType = spec.DataType,
                Name = spec.Name,
                Unit = spec.Unit,
                Key = spec.Key,
            };

            return categorySpecDto;
        }

        public async Task DeleteCategorySpecAsync(Guid categoryId, Guid specId)
        {
            var categorySpecEnt = await _specRepository.GetCategorySpecAsync(categoryId, specId);

            if (categorySpecEnt == null)
                throw new NotFoundException("This spec is not on this category's form");

            _specRepository.DeleteCategorySpec(categorySpecEnt);

            await _specRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryFormSpecDto>> GetCategoryFormAsync(Guid categoryId)
        {
            // 404 only when the category itself is missing.
            // A category with no form (or a parent category) is a valid answer: 200 + [].
            if (!await _categoryRepository.IsCategoryExistsAsync(categoryId))
                throw new NotFoundException("Category not found");

            return await _specQueries.GetCategoryFormAsync(categoryId);
        }

        public async Task<SpecOptionDto> CreateSpecOptionAsync(Guid specId, CreateSpecOptionDto createSpecOptionDto)
        {
            var spec = await _specRepository.GetSpecAttributeByIdAsync(specId);

            if (spec == null)
                throw new NotFoundException("Spec not found");

            // Only questions with a fixed list of choices can have options. vram_gb (Number) can't.
            if (spec.DataType != SpecDataType.Option && spec.DataType != SpecDataType.MultiOption)
                throw new BadRequestException($"'{spec.Key}' is a {spec.DataType} spec. Only Option and MultiOption specs can have choices");

            var value = createSpecOptionDto.Value.Trim();
            var slug = SlugHelper.Create(value);

            // A value with no a-z or 0-9 gives an empty slug.
            if (slug.Length == 0)
                throw new BadRequestException("The value must contain at least one English letter or digit");

            // The same value always gives the same slug, so this one check also catches a repeated value.
            if (await _specRepository.IsOptionSlugExistAsync(specId, slug))
                throw new BadRequestException($"This spec already has a choice with the slug '{slug}'");

            var specOptionEnt = new SpecOption
            {
                SpecAttributeId = specId,
                Value = value,
                Slug = slug,
                DisplayOrder = createSpecOptionDto.DisplayOrder
            };

            await _specRepository.CreateSpecOptionAsync(specOptionEnt);

            await _specRepository.SaveChangesAsync();

            return _mapper.Map<SpecOptionDto>(specOptionEnt);
        }

        public async Task<IEnumerable<SpecAttributeDto>> GetAllSpecAttributesAsync()
        {
            return await _specQueries.GetAllSpecAttributesAsync();
        }
    }
}
