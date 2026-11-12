
using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Specs;
using Souqify.Application.Interfaces;

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
    }
}
