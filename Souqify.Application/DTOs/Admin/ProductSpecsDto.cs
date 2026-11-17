
namespace Souqify.Application.DTOs.Admin
{
    // GET and PUT /api/admin/products/{productId}/specs return this.
    public class ProductSpecsDto
    {
        // The product's RowVersion. The page sends it back in the next PUT.
        public uint RowVersion { get; set; }

        // The product's answers, in the same shape as the PUT body, so the page can send them back as they are.
        public List<UpdateProductSpecValueDto> Values { get; set; } = new List<UpdateProductSpecValueDto>();
    }
}
