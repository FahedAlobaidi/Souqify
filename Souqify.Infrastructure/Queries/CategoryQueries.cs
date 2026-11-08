using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Souqify.Application.DTOs.Category;


namespace Souqify.Infrastructure.Queries
{
    public class CategoryQueries : ICategoryQueries
    {
        private const string AllCategories = "categories";

        private readonly SouqifyDbContext _souqifyDbContext;
        private readonly IMemoryCache _memoryCache;

        public CategoryQueries(SouqifyDbContext souqifyDbContext, IMemoryCache memoryCache)
        {
            _souqifyDbContext = souqifyDbContext;
            _memoryCache = memoryCache;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _memoryCache.GetOrCreateAsync(AllCategories, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var collection = _souqifyDbContext.Categories.AsNoTracking().Where(c => c.IsActive);

                return await collection.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Name = c.Name,
                    Slug = c.Slug,
                    DisplayOrder = c.DisplayOrder,
                    ParentId = c.ParentId,
                    ProductCount = c.Products.Where(p => p.IsActive).Count()
                }).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
            });

            return categories ?? new List<CategoryDto>();
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId)
        {
            var collection = _souqifyDbContext.Categories.AsNoTracking().Where(c => c.Id == categoryId);

            return await collection.Select(c => new CategoryDto
            {
                Id = c.Id,
                Description = c.Description,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                Slug = c.Slug,
                ParentId = c.ParentId,
                ProductCount = c.Products.Where(p => p.IsActive).Count()
            }).FirstOrDefaultAsync();
        }

        public void RemoveCategoriesCache()
        {
            _memoryCache.Remove(AllCategories);
        }
    }
}
