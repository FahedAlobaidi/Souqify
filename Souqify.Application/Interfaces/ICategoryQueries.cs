using Souqify.Application.DTOs.Category;

public interface ICategoryQueries
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid categoryId);
}
