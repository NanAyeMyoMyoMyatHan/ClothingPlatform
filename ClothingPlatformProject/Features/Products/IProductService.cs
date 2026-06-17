
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Product
{
    public interface IProductService
    {
        Task<int> InsertStepByStepAsync(ProductModel model);
        
        Task<PagedResult<ProductDto>> GetAllProduct(
    int page,
    int pageSize,string? search = null, int categoryId = 0);
       
        Task<bool> DeleteProductAsync(int id);
        //void CreateProductImage(ProductImage image);
        Task<PagedResult<BestSellerDto>> GetAllBestSellersAsync(
    int page, int pageSize, string? search = null, int categoryId = 0);

        Task<PagedResult<NewCreationDto>> GetAllNewCreationAsync(
            int page, int pageSize, string? search = null, int categoryId = 0);

        Task<bool> UpdateProductAsync(UpdateProductRequest request);
        Task<ProductDto?> GetByIdAsync(int id);

    }
}
