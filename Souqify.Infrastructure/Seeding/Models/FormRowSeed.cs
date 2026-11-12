
namespace Souqify.Infrastructure.Seeding.Models
{
    // One row of a form: which question, and its settings on this form.
    public class FormRowSeed
    {
        // The question's key, for example "vram_gb".
        public string Key { get; set; } = null!;

        public bool Required { get; set; }
        public bool Filterable { get; set; }
    }
}
