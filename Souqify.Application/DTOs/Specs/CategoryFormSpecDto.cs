
namespace Souqify.Application.DTOs.Specs
{
    // One row of a category's form, for GET /api/admin/categories/{categoryId}/specs.
    // It has everything CategorySpecDto has (the row's settings + the question's fields),
    // plus the question's choices, so the "add product" screen can fill its dropdowns.
    public class CategoryFormSpecDto : CategorySpecDto
    {
        // Number, Text and Bool questions have no choices: they send [] here, never null.
        public List<SpecOptionDto> Options { get; set; } = new();
    }
}
