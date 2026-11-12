
namespace Souqify.Application.DTOs.Admin
{
    // No SpecAttributeId here: the spec id comes from the URL (/categories/{categoryId}/specs/{specId}).
    public class UpdateCategorySpecDto
    {
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public int DisplayOrder { get; set; }
    }
}
