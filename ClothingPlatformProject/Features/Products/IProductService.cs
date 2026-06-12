using ClothingPlatformProject.Models.Product;

namespace ClothingPlatformProject.Features.Product
{
    public interface IProductService
    {
        List<ProductModel> GetAllProducts();
        ProductDto? GetProductById(int id);
        Task CreateProduct(ProductModel model);
        void UpdateProduct(int id, ProductUpdateRequest model);
        void DeleteProduct(int id);
        //void CreateProductImage(ProductImage image);
    }
}
