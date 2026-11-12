

using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<bool> IsCategoryExistsAsync(Guid categoryId);
        Task<bool?> IsCategoryExistOrHasProductsAsync(Guid categoryId);
        Task<bool?> CategoryHasChildrenAsync(Guid categoryId);
        Task<bool> CategoryHasSpecsAsync(Guid categoryId);
        Task<bool> IsCategoryNameUnique(string name,Guid? parentId);
        Task<bool> IsCategorySlugExist(string slug);
        void AddCategoryAsync(Category category);
        Task<bool> SaveChangesAsync();
    }
}
