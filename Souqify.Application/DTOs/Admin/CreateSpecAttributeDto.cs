

using Souqify.Domain.Entities.Enums;

namespace Souqify.Application.DTOs.Admin
{
    public class CreateSpecAttributeDto
    {
        public string Key { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Unit { get; set; }
        public SpecDataType DataType { get; set; }
    }
}
