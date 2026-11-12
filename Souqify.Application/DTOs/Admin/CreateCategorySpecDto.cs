

namespace Souqify.Application.DTOs.Admin
{
    public class CreateCategorySpecDto
    {
        public Guid SpecAttributeId { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public int DisplayOrder { get; set; }
    }
}
