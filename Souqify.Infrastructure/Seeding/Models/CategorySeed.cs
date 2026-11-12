
namespace Souqify.Infrastructure.Seeding.Models
{
    // One line of "categories" in spec-definitions.json.
    public class CategorySeed
    {
        public string Slug { get; set; } = null!;
        public string Name { get; set; } = null!;

        // The parent's slug. null = a top-level category.
        public string? Parent { get; set; }

        public int DisplayOrder { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
