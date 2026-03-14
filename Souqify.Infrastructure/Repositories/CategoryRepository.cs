using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
using Souqify.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public CategoryRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _souqifyDbContext.Categories.ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(Guid categoryId)
        {
            return await _souqifyDbContext.Categories.FindAsync(categoryId);
        }
    }
}
