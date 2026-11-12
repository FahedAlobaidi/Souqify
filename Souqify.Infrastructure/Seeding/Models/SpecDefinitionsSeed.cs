
namespace Souqify.Infrastructure.Seeding.Models
{
    // The whole spec-definitions.json file: its 3 lists.
    public class SpecDefinitionsSeed
    {
        public List<CategorySeed> Categories { get; set; } = new();
        public List<SpecSeed> Specs { get; set; } = new();
        public List<FormSeed> Forms { get; set; } = new();
    }
}
