using Souqify.Application.DTOs.Category;

public interface ICategoryQueries
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    void RemoveCategoriesCache();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId);
}
