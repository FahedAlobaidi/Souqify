

namespace Souqify.Application.DTOs.Image
{
    public class CreateImageDto
    {
        public required string ImageUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsMain { get; set; }
    }
}
