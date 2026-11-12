

using Souqify.Application.DTOs.Specs;
using Souqify.Domain.Entities;

namespace Souqify.Application.Interfaces
{
    public interface ISpecRepository
    {
        Task CreateSpecAttributeAsync(SpecAttribute specAttribute);
        Task CreateSpecOptionAsync(SpecOption specOption);
        Task CreateCategorySpecAsync(CategorySpec categorySpec);
        Task<bool> IsKeyExistAsync(string key);
        Task<SpecAttribute?> GetSpecAttributeByIdAsync(Guid id);
        Task<bool> IsSpecAttributeExistAsync(Guid id);
        Task<bool> IsOptionSlugExistAsync(Guid specAttributeId, string slug);
        Task<CategorySpec?> GetCategorySpecAsync(Guid categoryId, Guid specAttributeId);
        Task<bool> IsCategorySpecExistAsync(Guid categoryId, Guid specAttributeId);
        void DeleteCategorySpec(CategorySpec categorySpec);
        Task<bool> SaveChangesAsync();
    }
}
