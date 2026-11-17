
namespace Souqify.Application.DTOs.Product
{
    // CAT-4 AC 5: one spec line on the public product page, like "Video Memory: 12 GB".
    public class ProductSpecDto
    {
        public string Key { get; set; } = string.Empty;     // "vram_gb", kept for filter links later (CAT-5)

        public string Name { get; set; } = string.Empty;    // "Video Memory"

        public string? Unit { get; set; }                   // "GB", or null

        public List<string> Values { get; set; } = new List<string>();   // one value, or several for a MultiOption question
    }
}
