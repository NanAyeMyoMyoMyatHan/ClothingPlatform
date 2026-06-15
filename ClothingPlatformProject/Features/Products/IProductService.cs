using ClothingPlatformProject.Models.Product;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Product
{
    public interface IProductService
    {
        Task<int> InsertStepByStepAsync(ProductModel model);
        ProductDto? GetProductById(int id);
        List<ProductModel> GetAllProducts();
        void UpdateProduct(int id, ProductUpdateRequest model);
        Task<bool> DeleteProductAsync(int id);
        //void CreateProductImage(ProductImage image);
        Task<List<BestSellerDto>> GetAllBestSellersAsync();
        Task<List<NewCreationDto>> GetAllNewCreationAsync();
    }
}
