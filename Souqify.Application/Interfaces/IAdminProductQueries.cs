using Souqify.Application.DTOs.Admin;


namespace Souqify.Application.Interfaces
{
    public interface IAdminProductQueries
    {
        Task<AdminProductDto?> GetAdminProductByIdAsync(Guid id);
        Task<IEnumerable<LowStockDto>> GetLowStockVariantsAsync();
        Task<AdminVariantDto?> GetAdminVariantByIdAsync(Guid productId,Guid varianId); 
    }
}
