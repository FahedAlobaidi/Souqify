
namespace Souqify.Application.DTOs.Specs
{
    public class SpecOptionDto
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = null!;   // "PCIe 4.0"
        public string Slug { get; set; } = null!;    // "pcie-4-0", made by the server
        public int DisplayOrder { get; set; }
    }
}
