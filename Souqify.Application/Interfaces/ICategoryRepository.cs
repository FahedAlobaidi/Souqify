

namespace Souqify.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<bool> IsCategoryExistsAsync(Guid categoryId);
    }
}
