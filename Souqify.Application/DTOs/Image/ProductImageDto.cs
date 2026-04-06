namespace Souqify.Application.DTOs.Image
{
    public class ProductImageDto
    {
        public Guid Id { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsMain { get; set; }
    }
}
