using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public CategoryRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public void AddCategoryAsync(Category category)
        {
            _souqifyDbContext.Categories.Add(category);
        }

        public async Task<bool> IsCategoryExistsAsync(Guid categoryId)
        {
            return await _souqifyDbContext.Categories.AnyAsync(c => c.Id == categoryId);
        }
        /// <summary>
        /// true: category exist and holds products.
        /// false: category exist and dont hold products.
        /// null: category doesnt exist.
        /// </summary>
        public async Task<bool?> IsCategoryExistOrHasProductsAsync(Guid categoryId)
        {
            return await _souqifyDbContext.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => (bool?)c.Products.Any())
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// true: category exist and holds children categories.
        /// false: category exist and dont hold children categories.
        /// null: category doesnt exist.
        /// </summary>
        public async Task<bool?> CategoryHasChildrenAsync(Guid categoryId)
        {
            return await _souqifyDbContext.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => (bool?)c.Children.Any())
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsCategoryNameUnique(string name,Guid? parentId)
        {
            return await _souqifyDbContext.Categories.AnyAsync(c => c.Name == name && c.ParentId==parentId);
        }

        public async Task<bool> IsCategorySlugExist(string slug)
        {
            return await _souqifyDbContext.Categories.AnyAsync(c => c.Slug == slug);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }
    }
}
