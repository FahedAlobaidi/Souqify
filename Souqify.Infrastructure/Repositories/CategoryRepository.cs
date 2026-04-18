using Microsoft.EntityFrameworkCore;
using Souqify.Application.Interfaces;
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

        public async Task<bool> IsCategoryExistsAsync(Guid categoryId)
        {
            return await _souqifyDbContext.Categories.AnyAsync(c => c.Id == categoryId);
        }
    }
}
