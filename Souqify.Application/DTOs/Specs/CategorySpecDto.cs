
using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.DTOs.Specs
{
    public class CategorySpecDto
    {
        public Guid SpecAttributeId { get; set; }
        public string Key { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Unit { get; set; }
        public SpecDataType DataType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public int DisplayOrder { get; set; }
    }
}
