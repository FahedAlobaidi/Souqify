
namespace Souqify.Domain.Entities
{
    public class SpecOption
    {
        public Guid Id { get; set; }
        public Guid SpecAttributeId { get; set; }  // which question this choice belongs to

        public required string Value { get; set; } // "AM5" — what people read
        public required string Slug { get; set; }  // "am5" — used in web addresses

        public int DisplayOrder { get; set; }      // order of the checkboxes

        public SpecAttribute? SpecAttribute { get; set; }
    }
}
