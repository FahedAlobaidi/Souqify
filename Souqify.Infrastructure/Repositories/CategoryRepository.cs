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

        public async Task<bool> IsCategoryNameUnique(string name)
        {
            return await _souqifyDbContext.Categories.AnyAsync(c => c.Name == name);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }
    }
}
