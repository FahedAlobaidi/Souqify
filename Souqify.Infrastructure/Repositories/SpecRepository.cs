using Microsoft.EntityFrameworkCore;
using Souqify.Application.DTOs.Specs;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;

namespace Souqify.Infrastructure.Repositories
{
    public class SpecRepository : ISpecRepository
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public SpecRepository(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task CreateSpecAttributeAsync(SpecAttribute specAttribute)
        {
            await _souqifyDbContext.AddAsync(specAttribute);
        }

        public async Task CreateCategorySpecAsync(CategorySpec categorySpec)
        {
            await _souqifyDbContext.AddAsync(categorySpec);
        }

        public async Task<bool> IsKeyExistAsync(string key)
        {
            return await _souqifyDbContext.SpecAttributes.AnyAsync(a => a.Key == key);
        }

        public async Task CreateSpecOptionAsync(SpecOption specOption)
        {
            await _souqifyDbContext.AddAsync(specOption);
        }

        public async Task<SpecAttribute?> GetSpecAttributeByIdAsync(Guid id)
        {
            return await _souqifyDbContext.SpecAttributes.FindAsync(id);
        }

        public async Task<bool> IsSpecAttributeExistAsync(Guid id)
        {
            return await _souqifyDbContext.SpecAttributes.AnyAsync(sa => sa.Id == id);
        }

        public async Task<bool> IsOptionSlugExistAsync(Guid specAttributeId, string slug)
        {
            return await _souqifyDbContext.SpecOptions
                .AnyAsync(o => o.SpecAttributeId == specAttributeId && o.Slug == slug);
        }

        // One form row, with its question loaded (the service needs DataType and the response needs Key, Name, Unit).
        public async Task<CategorySpec?> GetCategorySpecAsync(Guid categoryId, Guid specAttributeId)
        {
            return await _souqifyDbContext.CategorySpecs
                .Include(cs => cs.SpecAttribute)
                .FirstOrDefaultAsync(cs => cs.CategoryId == categoryId && cs.SpecAttributeId == specAttributeId);
        }

        // Yes/no only: is this question already on this category's form? Nothing is loaded or tracked.
        public async Task<bool> IsCategorySpecExistAsync(Guid categoryId, Guid specAttributeId)
        {
            return await _souqifyDbContext.CategorySpecs
                .AnyAsync(cs => cs.CategoryId == categoryId && cs.SpecAttributeId == specAttributeId);
        }

        public void DeleteCategorySpec(CategorySpec categorySpec)
        {
            _souqifyDbContext.CategorySpecs.Remove(categorySpec);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _souqifyDbContext.SaveChangesAsync() > 0;
        }
    }
}
