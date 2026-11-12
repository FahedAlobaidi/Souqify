
using Souqify.Domain.Entities.Enums;

namespace Souqify.Infrastructure.Seeding.Models
{
    // One question of "specs" in spec-definitions.json.
    public class SpecSeed
    {
        public string Key { get; set; } = null!;
        public string Name { get; set; } = null!;
        public SpecDataType DataType { get; set; }

        // Left out in the file = null (no unit).
        public string? Unit { get; set; }

        // Only Option and MultiOption questions have choices.
        // Left out in the file = this empty list stays.
        // A choice's slug and DisplayOrder are not in the file: the seeder makes them.
        public List<string> Options { get; set; } = new();
    }
}
