using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.User;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

//using ClothingPlatformProject.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Product
{
    public class ProductServices: IProductService
    {
        private readonly AppDbContext _db;
        private readonly  IWebHostEnvironment _env;
        public ProductServices(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<int> InsertStepByStepAsync(ProductModel model)
        {
            using var context = new AppDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var newProduct = new ClothingPlatform.DB.AppDbModels.Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    BasePrice = model.BasePrice
                };
                context.Products.Add(newProduct);
                await context.SaveChangesAsync();

                int newProductId = newProduct.ProductId;
                string imageUrl = "stdCoat.jpg";

                // 🌟 WebRootPath မရှိရင် ContentRootPath ထဲက wwwroot ကို လှမ်းယူခိုင်းလိုက်တာပါ (စိတ်အချရဆုံး)
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

                if (!string.IsNullOrEmpty(model.ImageBase64) && !string.IsNullOrEmpty(model.ImageFileName))
                {
                    // အပေါ်က ရှာဖွေထားတဲ့ webRootPath ကို သုံးမယ်
                    var folder = Path.Combine(webRootPath, "images", "products");

                    // Folder အဆင့်ဆင့် ရှိမရှိ သေချာအောင် စစ်ပြီး မရှိရင် ဆောက်မယ်
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    var fileName = $"{Guid.NewGuid()}_{model.ImageFileName}";
                    var filePath = Path.Combine(folder, fileName);

                    var bytes = Convert.FromBase64String(model.ImageBase64);
                    await File.WriteAllBytesAsync(filePath, bytes);

                    imageUrl = fileName;
                }

                context.ProductImages.Add(new ProductImage
                {
                    ProductId = newProductId,
                    ImageUrl = imageUrl,
                    IsPrimary = true
                });

                if (model.VariantsDto != null && model.VariantsDto.Any())
                {
                    var newVariants = model.VariantsDto.Select(v => new ProductVariant
                    {
                        ProductId = newProductId,
                        Size = v.Size,
                        Color = v.Color,
                        StockQuantity = v.StockQuantity,
                        Sku = $"{model.Name.Replace(" ", "").ToUpper()}-{v.Size.Replace(" ", "").ToUpper()}-{v.Color.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}"
                    }).ToList();

                    context.ProductVariants.AddRange(newVariants);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return newProductId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Database Save Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var dto = await _db.Products
                .Include(p=>p.Category)
             .Include(p => p.ProductVariants)
            .Include(p => p.ProductImages)
            .Where(p => p.ProductId == id)
            .Select(p => new ProductDto
             {
        Id = p.ProductId,
        Name = p.Name,
        Description = p.Description,
        BasePrice = p.BasePrice,
        CategoryName = p.Category.Name,
        CategoryId = p.CategoryId,
        VariantsDto = p.ProductVariants.Select(v => new VariantDto
        {
            VariantId = v.VariantId,
            Size = v.Size,
            Color = v.Color,
            StockQuantity = v.StockQuantity
        }).ToList(),

        ImageDto = p.ProductImages
            .Where(i => i.IsPrimary == true)
            .Select(i => i.ImageUrl)
            .FirstOrDefault()
    })
    .FirstOrDefaultAsync();
       
            return dto;
        
  }

        public async Task<bool> UpdateProductAsync(UpdateProductRequest request)
        {
            using var context = new AppDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // ၁။ ပြင်မယ့် Product ရှိ၊ မရှိ ID ဖြင့် အရင်ရှာမယ် (ဒီနေရာမှာ model.ProductId ပါလာရပါမယ်)
                var existingProduct = await context.Products.FindAsync(request.Id);
                if (existingProduct == null)
                {
                    throw new Exception($"Product with ID {request.Id} not found.");
                }

                // ၂။ အချက်အလက်အသစ်များ အစားထိုး ပြင်ဆင်ခြင်း
                existingProduct.Name = request.Name;
                existingProduct.Description = request.Description;
                existingProduct.CategoryId = request.CategoryId;
                existingProduct.BasePrice = request.BasePrice;

                // ၃။ Image ကို စစ်ဆေးပြီး အစားထိုး ပြင်ဆင်ခြင်း
                // User က ပုံအသစ် ရွေးပေးလိုက်မှသာ (Base64 ပါလာမှသာ) ပုံအသစ် သွားသိမ်းမယ်
                if (!string.IsNullOrEmpty(request.ImageBase64) && !string.IsNullOrEmpty(request.ImageFileName))
                {
                    var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                    var folder = Path.Combine(webRootPath, "images", "products");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    var fileName = $"{Guid.NewGuid()}_{request.ImageFileName}";
                    var filePath = Path.Combine(folder, fileName);

                    var bytes = Convert.FromBase64String(request.ImageBase64);
                    await File.WriteAllBytesAsync(filePath, bytes);

                    try { File.SetLastWriteTime(filePath, DateTime.Now.AddSeconds(-5)); } catch { }

                    var existingImage = context.ProductImages.FirstOrDefault(i => i.ProductId == request.Id && i.IsPrimary == true);

                    if (existingImage != null)
                    {
                        existingImage.ImageUrl = fileName;
                    }
                    else
                    {
                        context.ProductImages.Add(new ProductImage
                        {
                            ProductId = request.Id,
                            ImageUrl = fileName,
                            IsPrimary = true
                        });
                    }
                }

                var oldVariants = context.ProductVariants.Where(v => v.ProductId == request.Id).ToList();
                if (oldVariants.Any())
                {
                    context.ProductVariants.RemoveRange(oldVariants);
                }

                if (request.VariantsDto != null && request.VariantsDto.Any())
                {
                    var newVariants = request.VariantsDto.Select(v => {
                        var safeSize = string.IsNullOrWhiteSpace(v.Size) ? "FREE" : v.Size.Trim();
                        var safeColor = string.IsNullOrWhiteSpace(v.Color) ? "MIX" : v.Color.Trim();
                        var safeProdName = string.IsNullOrWhiteSpace(request.Name) ? "PROD" : request.Name.Replace(" ", "");

                        var sku = $"{safeProdName.ToUpper()}-{safeSize.Replace(" ", "").ToUpper()}-{safeColor.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}";

                        return new ProductVariant
                        {
                            ProductId = request.Id, // သက်ဆိုင်ရာ Product ID အဟောင်းအတိုင်း ထည့်မယ်
                            Size = safeSize,
                            Color = safeColor,
                            StockQuantity = v.StockQuantity,
                            Sku = sku
                        };
                    }).ToList();

                    context.ProductVariants.AddRange(newVariants);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true; // အောင်မြင်ရင် true ပြန်မယ်
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Database Update Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _db.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return false;

            var variantIds = product.ProductVariants.Select(v => v.VariantId).ToList();

            var relatedOrderItems = _db.OrderItems
                .Where(oi => variantIds.Contains(oi.VariantId));
            var relatedSalesLogs = _db.StaffSalesLogs.Where(x => variantIds.Contains(x.VariantId));
            _db.StaffSalesLogs.RemoveRange(relatedSalesLogs);
            
            _db.OrderItems.RemoveRange(relatedOrderItems);
            _db.ProductImages.RemoveRange(product.ProductImages);
            _db.ProductVariants.RemoveRange(product.ProductVariants);
            _db.Products.Remove(product);

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<PagedResult<BestSellerDto>> GetAllBestSellersAsync(
    int page, int pageSize, string? search = null, int categoryId = 0)
        {
            var query = _db.OrderItems
                .AsNoTracking()
                .Where(x => x.Variant != null && x.Variant.Product != null); // safety guard

            // Filter BEFORE GroupBy
            if (categoryId > 0)
                query = query.Where(x => x.Variant.Product.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    x.Variant.Product.Name.Contains(search) ||
                    (x.Variant.Product.Description != null &&
                     x.Variant.Product.Description.Contains(search)));

            var grouped = query
                .GroupBy(x => x.Variant.ProductId)
                .Select(g => new BestSellerDto
                {
                    ProductId = g.Key,
                    Name = g.First().Variant.Product.Name,
                    TotalSold = g.Sum(x => x.Quantity),
                    BasePrice = g.First().Variant.Product.BasePrice,
                    CategoryName = g.First().Variant.Product.Category.Name,
                    Description = g.First().Variant.Product.Description ?? string.Empty,
                    ImageDto = g.First().Variant.Product.ProductImages
                        .Where(i => i.IsPrimary == true)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? g.First().Variant.Product.ProductImages
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault(),
                    VariantsDto = g.First().Variant.Product.ProductVariants
                        .Select(v => new VariantDto
                        {
                            VariantId = v.VariantId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.TotalSold);

            var totalCount = await grouped.CountAsync();

            var items = await grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BestSellerDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<NewCreationDto>> GetAllNewCreationAsync(
    int page, int pageSize, string? search = null, int categoryId = 0)
        {
            var query = _db.Products
                .AsNoTracking()
                .AsQueryable();

            // Filters FIRST
            if (categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.ProductId) // order AFTER filtering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new NewCreationDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    Description = p.Description ?? string.Empty,
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    ImageDto = p.ProductImages
                        .Where(i => i.IsPrimary == true)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault(),
                    VariantsDto = p.ProductVariants
                        .Select(v => new VariantDto
                        {
                            VariantId = v.VariantId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity
                        })
                        .ToList()
                })
                .ToListAsync();

            return new PagedResult<NewCreationDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<ProductDto>> GetAllProduct(
    int page, int pageSize, string? search = null, int categoryId = 0)
        {
            var query = _db.Products
                .AsNoTracking()
                .AsQueryable();

            // Apply filters BEFORE count and pagination
            if (categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));

            var totalCount = await query.CountAsync(); // count AFTER filtering

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    Description = p.Description ?? string.Empty,
                    CategoryName = p.Category != null
                        ? p.Category.Name
                        : "General",
                    ImageDto = p.ProductImages
                        .Where(i => i.IsPrimary == true)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault(),
                    VariantsDto = p.ProductVariants
                        .Select(v => new VariantDto
                        {
                            VariantId = v.VariantId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity
                        })
                        .ToList()
                })
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
   

}
