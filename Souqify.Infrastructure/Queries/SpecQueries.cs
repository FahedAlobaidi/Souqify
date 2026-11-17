
using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Specs;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Infrastructure.Queries
{
    public class SpecQueries : ISpecQueries
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public SpecQueries(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<IEnumerable<SpecAttributeDto>> GetAllSpecAttributesAsync()
        {
            var collection = _souqifyDbContext.SpecAttributes.AsNoTracking();

            var specAttributeDtos = collection.OrderBy(sa => sa.Name).ThenBy(sa=>sa.Key).Select(sa => new SpecAttributeDto
            {
                Id = sa.Id,
                Key = sa.Key,
                Name = sa.Name,
                DataType = sa.DataType,
                Unit = sa.Unit,
                Options = sa.Options.OrderBy(o => o.DisplayOrder).ThenBy(o => o.Value).Select(o => new SpecOptionDto
                {
                    Id = o.Id,
                    DisplayOrder = o.DisplayOrder,
                    Slug = o.Slug,
                    Value = o.Value,
                }).ToList(),
            });

            return await specAttributeDtos.ToListAsync();
        }

        public async Task<IEnumerable<CategoryFormSpecDto>> GetCategoryFormAsync(Guid categoryId)
        {
            var collection = _souqifyDbContext.CategorySpecs.AsNoTracking().Where(cs => cs.CategoryId == categoryId);

            var categorySpecDtos = collection.OrderBy(cs => cs.DisplayOrder).ThenBy(cs => cs.SpecAttribute!.Key).Select(cs => new CategoryFormSpecDto
            {
                SpecAttributeId = cs.SpecAttributeId,
                DisplayOrder = cs.DisplayOrder,
                IsFilterable = cs.IsFilterable,
                IsRequired = cs.IsRequired,

                Key = cs.SpecAttribute!.Key,
                Name = cs.SpecAttribute.Name,
                DataType = cs.SpecAttribute.DataType,
                Unit = cs.SpecAttribute.Unit,

                Options = cs.SpecAttribute.Options.OrderBy(o => o.DisplayOrder).ThenBy(o => o.Value).Select(o => new SpecOptionDto
                {
                    Id = o.Id,
                    DisplayOrder = o.DisplayOrder,
                    Slug = o.Slug,
                    Value = o.Value,
                }).ToList()
            });

            return await categorySpecDtos.ToListAsync();
        }

        // CAT-4: Fahed writes this one.
        // Return null for an unknown product (the service turns it into a 404). Otherwise the product's RowVersion and its answers,
        // in the same shape as the PUT body: one item per question, a MultiOption question's choices together in Options.
        public async Task<ProductSpecsDto?> GetProductSpecsAsync(Guid productId)
        {
            var product = await _souqifyDbContext.Products.Where(p => p.Id == productId).Select(p => new { p.RowVersion}).FirstOrDefaultAsync();

            if (product == null)
                return null;

            var specValues = await _souqifyDbContext.ProductSpecValues.AsNoTracking().Where(sv => sv.ProductId == productId).Include(sv=>sv.Option).Include(pv=>pv.SpecAttribute).ToListAsync();

            List<UpdateProductSpecValueDto> updateSpecValues= new List<UpdateProductSpecValueDto>();
           

            foreach (var value in specValues)
            {
                if (value.SpecAttribute!.DataType != SpecDataType.MultiOption)
                {
                    UpdateProductSpecValueDto update = new UpdateProductSpecValueDto();
                    update.Key = value.SpecAttribute.Key;

                    switch (value.SpecAttribute.DataType)
                    {
                        case SpecDataType.Option:
                            update.Option = value.Option!.Slug;
                            break;
                        case SpecDataType.Number:
                            update.Number = value.NumberValue;
                            break;
                        case SpecDataType.Boolean:
                            update.Bool = value.BoolValue;
                            break;
                        case SpecDataType.Text:
                            update.Text = value.TextValue;
                            break;
                    }

                    updateSpecValues.Add(update);
                }
                else
                {
                    UpdateProductSpecValueDto? update = updateSpecValues.FirstOrDefault(u => u.Key == value.SpecAttribute.Key);

                    if (update == null)
                    {
                        // This question's first row: make its item with an empty list.
                        update = new UpdateProductSpecValueDto();
                        update.Key = value.SpecAttribute.Key;
                        update.Options = new List<string>();
                        updateSpecValues.Add(update);
                    }

                    update.Options!.Add(value.Option!.Slug);
                }
            }
            return new ProductSpecsDto { RowVersion = product.RowVersion, Values = updateSpecValues };
        }
    }
}
