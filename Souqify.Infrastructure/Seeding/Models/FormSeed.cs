
namespace Souqify.Infrastructure.Seeding.Models
{
    // One form of "forms" in spec-definitions.json: a category and its rows.
    public class FormSeed
    {
        // The category's slug.
        public string Category { get; set; } = null!;

        // The rows in display order: the 1st row gets DisplayOrder 1, and so on.
        public List<FormRowSeed> Specs { get; set; } = new();
    }
}
