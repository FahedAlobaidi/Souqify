
using Souqify.Application.DTOs.Specs;

namespace Souqify.Application.Interfaces
{
    public interface ISpecQueries
    {
        Task<IEnumerable<SpecAttributeDto>> GetAllSpecAttributesAsync();
        Task<IEnumerable<CategoryFormSpecDto>> GetCategoryFormAsync(Guid categoryId);
    }
}
