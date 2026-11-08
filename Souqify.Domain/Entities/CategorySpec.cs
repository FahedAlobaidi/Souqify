
namespace Souqify.Domain.Entities
{
    public class CategorySpec
    {
        public Guid CategoryId { get; set; }
        public Guid SpecAttributeId { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public int DisplayOrder { get; set; }

        public Category? Category { get; set; }
        public SpecAttribute? SpecAttribute { get; set; }
    }
}
