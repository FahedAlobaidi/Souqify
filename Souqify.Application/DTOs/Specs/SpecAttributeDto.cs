

using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.DTOs.Specs
{
    public class SpecAttributeDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Unit { get; set; }
        public SpecDataType DataType { get; set; }
        public List<SpecOptionDto> Options { get; set; } = new();
    }
}
