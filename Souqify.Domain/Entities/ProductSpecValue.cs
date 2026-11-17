
namespace Souqify.Domain.Entities
{
    public class ProductSpecValue
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }        // which product this answer belongs to
        public Guid SpecAttributeId { get; set; }  // which question it answers

        // Exactly one of these 4 is filled, depending on the question's DataType.
        public Guid? OptionId { get; set; }        // Option / MultiOption: the chosen choice
        public decimal? NumberValue { get; set; }  // Number: 12, 5.4 (the unit stays on the question)
        public bool? BoolValue { get; set; }       // Boolean: true / false
        public string? TextValue { get; set; }     // Text: "1x 16-pin"

        public Product? Product { get; set; }
        public SpecAttribute? SpecAttribute { get; set; }
        public SpecOption? Option { get; set; }
    }
}
