
using Souqify.Application.DTOs.Admin;
using Souqify.Application.DTOs.Specs;

namespace Souqify.Application.Interfaces
{
    public interface ISpecService
    {
        Task<SpecAttributeDto> CreateSpecAttributeAsync(CreateSpecAttributeDto createSpecAttributeDto);
        Task<CategorySpecDto> CreateCategorySpecAsync(Guid categoryId, CreateCategorySpecDto createCategorySpecDto);
        Task<CategorySpecDto> UpdateCategorySpecAsync(Guid categoryId, Guid specId, UpdateCategorySpecDto updateCategorySpecDto);
        Task DeleteCategorySpecAsync(Guid categoryId, Guid specId);
        Task<IEnumerable<CategoryFormSpecDto>> GetCategoryFormAsync(Guid categoryId);
        Task<SpecOptionDto> CreateSpecOptionAsync(Guid specId, CreateSpecOptionDto createSpecOptionDto);
        Task<IEnumerable<SpecAttributeDto>> GetAllSpecAttributesAsync();
    }
}
