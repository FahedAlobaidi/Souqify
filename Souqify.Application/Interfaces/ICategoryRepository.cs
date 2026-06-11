

using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<bool> IsCategoryExistsAsync(Guid categoryId);
        Task<bool> IsCategoryNameUnique(string name);
        void AddCategoryAsync(Category category);
        Task<bool> SaveChangesAsync();
    }
}
