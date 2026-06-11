using ClothingPlatformProject.Models.Product;

namespace ClothingPlatformProject.Features.Product
{
    public interface IProductService
    {
        List<ProductModel> GetAllProducts();
        ProductDto? GetProductById(int id);
        void CreateProduct(ProductCreateRequest model);
        void UpdateProduct(int id, ProductUpdateRequest model);
        void DeleteProduct(int id);
    }
}
