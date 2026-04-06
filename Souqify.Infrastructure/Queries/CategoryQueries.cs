using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Category;


namespace Souqify.Infrastructure.Queries
{
    public class CategoryQueries : ICategoryQueries
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public CategoryQueries(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var collection = _souqifyDbContext.Categories.AsNoTracking().Where(c => c.IsActive);

            return await collection.Select(c => new CategoryDto
            {
                Id = c.Id,
                Description = c.Description,
                Name = c.Name,
                ProductCount = c.Products.Where(p => p.IsActive).Count()
            }).ToListAsync();
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId)
        {
            var collection = _souqifyDbContext.Categories.AsNoTracking().Where(c => c.Id == categoryId);

            return await collection.Select(c => new CategoryDto
            {
                Id = c.Id,
                Description = c.Description,
                Name = c.Name,
                ProductCount = c.Products.Where(p => p.IsActive).Count()
            }).FirstOrDefaultAsync();
        }
    }
}
