using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Product
{
    public class ProductServices: IProductService
    {
        private readonly AppDbContext _db;

        public ProductServices(AppDbContext db)
        {
            _db = db;
        }

        public List<ProductModel> GetAllProducts()
        {
            return _db.Products.AsNoTracking()
                .Include(p=>p.ProductVariants)
                .Select(x => new ProductModel
                {
                    Id = x.ProductId,
                    Name = x.Name,
                    Description = x.Description,
                    CategoryId = x.CategoryId,
                    BasePrice = x.BasePrice,
                    // Product အောက်က မတူညီတဲ့ Size / Color များကို တစ်ခါတည်း ဆွဲထုတ်ခြင်း
                    Variants = _db.ProductVariants.Where(v => v.ProductId == x.ProductId)
                        .Select(v => new VariantDto
                        {
                            VariantId = v.VariantId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity
                        }).ToList()
                }).ToList();
        }

        public ProductDto? GetProductById(int id)
        {
            var product = _db.Products.AsNoTracking().FirstOrDefault(x => x.ProductId == id);
            if (product == null) return null;

            return new ProductDto
            {
                Id = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                BasePrice = product.BasePrice
            };
        }

        public void CreateProduct(ProductCreateRequest model)
        {
            var product = new ClothingPlatform.DB.AppDbModels.Product
            {
                Name = model.Name,
                Description = model.Description,
                CategoryId = model.CategoryId,
                BasePrice = model.BasePrice
            };
            _db.Products.Add(product);
            _db.SaveChanges();
        }

        public void UpdateProduct(int id, ProductUpdateRequest model)
        {
            var item = _db.Products.FirstOrDefault(x => x.ProductId == id);
            if (item == null) return;

            item.Name = model.Name;
            item.Description = model.Description;
            item.CategoryId = model.CategoryId;
            item.BasePrice = model.BasePrice;

            _db.SaveChanges();
        }

        public void DeleteProduct(int id)
        {
            var item = _db.Products.FirstOrDefault(x => x.ProductId == id);
            if (item == null) return;

            _db.Products.Remove(item);
            _db.SaveChanges();
        }
    }
}
