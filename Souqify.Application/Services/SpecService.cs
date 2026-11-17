
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
        private readonly IProductRepository _productRepository;

        public SpecService(IMapper mapper, ISpecRepository specRepository, ISpecQueries specQueries, ICategoryRepository categoryRepository,
            IProductRepository productRepository)
        {
            _mapper = mapper;
            _specRepository = specRepository;
            _specQueries = specQueries;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
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

            // AC 6: a question that just arrived on the form has no answers yet, so every product in the category would be missing it.
            if (createCategorySpecDto.IsRequired && await _categoryRepository.IsCategoryExistOrHasProductsAsync(categoryId) == true)
                throw new ConflictException($"'{specAttributeEnt.Key}' can't be added as required, because this category already has products and none of them has an answer for it. Add it as optional, fill the products' answers, then make it required.");

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

            // AC 6: only when the row goes from optional to required, and some product in this category has no answer for it.
            bool becomingRequired = !categorySpecEnt.IsRequired && updateCategorySpecDto.IsRequired;

            if (becomingRequired && !await _productRepository.AreAllProductsAnsweredAsync(categoryId, categorySpecEnt.SpecAttributeId))
                throw new ConflictException($"'{spec.Key}' can't be made required, because some products in this category have no answer for it. Fill their answers first, then make it required.");

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

            // AC 6: removing it would leave answers whose question isn't on their product's form anymore.
            if (await _specRepository.IsSpecAnsweredInCategoryAsync(categoryId, categorySpecEnt.SpecAttributeId))
                throw new ConflictException($"'{categorySpecEnt.SpecAttribute!.Key}' can't be removed, because products in this category still have answers for it. Remove those answers first, then remove the question.");

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

        // CAT-4 AC 7: delete one choice of a question.
        public async Task DeleteSpecOptionAsync(Guid specId, Guid optionId)
        {
            var specOptionEnt = await _specRepository.GetSpecOptionAsync(specId, optionId);

            if (specOptionEnt == null)
                throw new NotFoundException("This choice is not one of this spec's choices");

            // AC 6 rule 4: the database would refuse anyway (Restrict on OptionId), but with a 500 and no reason.
            if (await _specRepository.IsOptionInUseAsync(optionId))
                throw new ConflictException($"'{specOptionEnt.Value}' can't be deleted, because some products have chosen it. Change their answers first, then delete it.");

            _specRepository.DeleteSpecOption(specOptionEnt);

            await _specRepository.SaveChangesAsync();
        }

        // CAT-4 AC 7: remove all of one product's answers. Needed before moving a product to another category (AC 6 rule 3),
        // because the PUT can't send an empty list while the form has required questions.
        public async Task ClearProductSpecsAsync(Guid productId)
        {
            var productEnt = await _productRepository.GetProductWithSpecValuesAsync(productId);

            if (productEnt == null)
                throw new NotFoundException($"Product with id {productId} not found");

            // Nothing to clear: no write, so the product keeps its RowVersion and open pages stay valid.
            if (productEnt.SpecValues.Count == 0)
                return;

            // ProductId is required, so EF deletes the rows that leave this list (the same as the PUT).
            productEnt.SpecValues.Clear();

            // The product row changes too, so it gets a new RowVersion. A specs page opened before this gets a 409 on its next save.
            productEnt.UpdatedAt = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<SpecAttributeDto>> GetAllSpecAttributesAsync()
        {
            return await _specQueries.GetAllSpecAttributesAsync();
        }

        public async Task<ProductSpecsDto> GetProductSpecsAsync(Guid productId)
        {
            return await _specQueries.GetProductSpecsAsync(productId) ?? throw new NotFoundException($"Product with id {productId} not found");
        }

        // CAT-4 AC 2 + AC 4: Fahed writes this one (the save and the answer checks).
        // Return the answers with _specQueries.GetProductSpecsAsync, so the admin gets the new RowVersion.
        public async Task<ProductSpecsDto> UpdateProductSpecsAsync(Guid productId, UpdateProductSpecsDto updateProductSpecsDto)
        {
            var productEnt=await _productRepository.GetProductWithSpecValuesAsync(productId);

            if(productEnt==null)
                throw new NotFoundException($"Product with {productId} not found");

            if (updateProductSpecsDto.RowVersion != productEnt.RowVersion)
                throw new ConcurrencyConflictException("Someone changed this product's specs after you opened the page. Please refresh and try again.");

            var categoryForm=await _specQueries.GetCategoryFormAsync(productEnt.CategoryId);

            var productSpecDict = updateProductSpecsDto.Values.ToDictionary(sv => sv.Key);

            var categoryFormDict = categoryForm.ToDictionary(cf => cf.Key);

            // The new answer rows. Built while checking, saved only if every check passes.
            List<ProductSpecValue> newSpecValues = new List<ProductSpecValue>();

            foreach(var specValue in updateProductSpecsDto.Values)
            {
                //check that all answers key in the form
                if (!categoryFormDict.TryGetValue(specValue.Key, out var categorySpecForm))
                    throw new BadRequestException($"The answer with key {specValue.Key} is not for this category form questions");

                // Each case: the field this DataType needs must be filled, then the value is checked and its row is built.
                switch (categorySpecForm.DataType)
                {
                    case SpecDataType.Option:
                        {
                            if (specValue.Option == null)
                                throw new BadRequestException($"'{specValue.Key}' needs an option.");

                            var option = categorySpecForm.Options.FirstOrDefault(o => o.Slug == specValue.Option);
                            if (option == null)
                                throw new BadRequestException($"The option you choosed {specValue.Option} is not one of the available choices");

                            newSpecValues.Add(new ProductSpecValue { ProductId = productEnt.Id, SpecAttributeId = categorySpecForm.SpecAttributeId, OptionId = option.Id });
                        }
                        break;
                    case SpecDataType.MultiOption:
                        {
                            if (specValue.Options == null)
                                throw new BadRequestException($"'{specValue.Key}' needs options.");

                            // One row per chosen slug, so each slug is looked up on its own: its Id becomes that row's OptionId.
                            foreach (var slug in specValue.Options)
                            {
                                var option = categorySpecForm.Options.FirstOrDefault(o => o.Slug == slug);
                                if (option == null)
                                    throw new BadRequestException($"The option you choosed {slug} is not one of the available choices");

                                newSpecValues.Add(new ProductSpecValue { ProductId = productEnt.Id, SpecAttributeId = categorySpecForm.SpecAttributeId, OptionId = option.Id });
                            }
                        }
                        break;
                    case SpecDataType.Number:
                        if (specValue.Number == null)
                            throw new BadRequestException($"'{specValue.Key}' needs a number.");

                        newSpecValues.Add(new ProductSpecValue { ProductId = productEnt.Id, SpecAttributeId = categorySpecForm.SpecAttributeId, NumberValue = specValue.Number });
                        break;
                    case SpecDataType.Boolean:
                        if (specValue.Bool == null)
                            throw new BadRequestException($"'{specValue.Key}' needs true or false.");

                        newSpecValues.Add(new ProductSpecValue { ProductId = productEnt.Id, SpecAttributeId = categorySpecForm.SpecAttributeId, BoolValue = specValue.Bool });
                        break;
                    case SpecDataType.Text:
                        if (specValue.Text == null)
                            throw new BadRequestException($"'{specValue.Key}' needs a text.");

                        newSpecValues.Add(new ProductSpecValue { ProductId = productEnt.Id, SpecAttributeId = categorySpecForm.SpecAttributeId, TextValue = specValue.Text });
                        break;
                }
            }


            foreach(var categorySpecForm in categoryForm)
            {
                if(!productSpecDict.TryGetValue(categorySpecForm.Key, out var productSpec))
                {
                    if (categorySpecForm.IsRequired)
                        throw new BadRequestException($"The question with key {categorySpecForm.Key} is required");
                }
            }

            // Replace all. ProductId is required, so EF deletes the old rows that leave this list.
            productEnt.SpecValues.Clear();

            foreach (var newSpecValue in newSpecValues)
                productEnt.SpecValues.Add(newSpecValue);

            // The answers live in their own table, so the product row has to change too. That gives the product a new RowVersion,
            // and EF's UPDATE checks the old one, so a second admin saving at the same moment gets a 409.
            productEnt.UpdatedAt = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();

            return await GetProductSpecsAsync(productId);
        }
    }
}
