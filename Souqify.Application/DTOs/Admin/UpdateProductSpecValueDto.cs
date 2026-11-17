
namespace Souqify.Application.DTOs.Admin
{
    // One answer. Only the field that matches the question's DataType is filled.
    public class UpdateProductSpecValueDto
    {
        // Not "required": a missing key must reach the validator, so the 400 looks like every other one.
        public string Key { get; set; } = null!;     // "vram_gb"

        public string? Option { get; set; }          // Option question: the option's slug, "rtx-5070"
        public List<string>? Options { get; set; }   // MultiOption question: the slugs, ["atx", "micro-atx"]
        public decimal? Number { get; set; }         // Number question: 12
        public bool? Bool { get; set; }              // Boolean question: true
        public string? Text { get; set; }            // Text question: "1x 16-pin"
    }
}
