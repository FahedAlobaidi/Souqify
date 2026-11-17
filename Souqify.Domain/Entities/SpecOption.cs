
namespace Souqify.Domain.Entities
{
    public class SpecOption
    {
        public Guid Id { get; set; }
        public Guid SpecAttributeId { get; set; }  // which question this choice belongs to
        public string Value { get; set; } = null!; // "AM5" — what people read
        public string Slug { get; set; } = null!;  // "am5" — used in web addresses

        public int DisplayOrder { get; set; }      // order of the checkboxes

        public SpecAttribute? SpecAttribute { get; set; }
    }
}
