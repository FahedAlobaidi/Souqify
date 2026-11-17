
namespace Souqify.Application.DTOs.Admin
{
    // PUT /api/admin/products/{productId}/specs. The product id comes from the URL.
    public class UpdateProductSpecsDto
    {
        // The product's RowVersion from GET /api/admin/products/{productId}/specs. A stale one returns 409.
        public uint RowVersion { get; set; }

        // All of the product's answers. They replace the old answers.
        public List<UpdateProductSpecValueDto> Values { get; set; } = new List<UpdateProductSpecValueDto>();
    }
}
