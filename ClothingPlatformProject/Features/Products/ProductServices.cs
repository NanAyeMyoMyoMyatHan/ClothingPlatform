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

        public ProductServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> InsertStepByStepAsync(ProductModel model)
        {
            using (var context = new AppDbContext())
            {
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // ၁။ Main Product ကို အရင်ဆုံး တည်ဆောက်ခြင်း
                        var newProduct = new ClothingPlatform.DB.AppDbModels.Product
                        {
                            Name = model.Name,
                            Description = model.Description,
                            CategoryId = model.CategoryId,
                            BasePrice = model.BasePrice
                            // 💡 တကယ်လို့ Database Table ထဲမှာ CreatedAt သို့မဟုတ် IsFeatured တွေ NOT NULL ဖြစ်နေရင် ဤနေရာတွင် တစ်ပါတည်း ထည့်ပေးရပါမည်
                        };

                        context.Products.Add(newProduct);
                        await context.SaveChangesAsync(); // SQL က Auto-Increment ID ထုတ်ပေးရန် ပထမအကြိမ် သိမ်းခြင်း

                        // ⚠️ သတိပြုရန်- သင့် Product Table ရဲ့ PK နာမည်သည် 'Id' ဖြစ်ပါက newProduct.Id ဟု ပြောင်းရေးပေးပါ
                        int newProductId = newProduct.ProductId;

                        // ၂။ Image ရှိပါက ProductId နှင့် ချိတ်ဆက်ထည့်သွင်းခြင်း
                        if (model.ImageDto != null)
                        {
                            var newImage = new ProductImage
                            {
                                ProductId = newProductId,
                                ImageUrl = model.ImageDto.ImageUrl,
                                IsPrimary = true  // ← ဒါထည့်ပါ
                            };
                            context.ProductImages.Add(newImage);
                        }

                        // ၃။ Variants များ ရှိပါက ProductId နှင့် ချိတ်ဆက်ပြီး Auto-SKU တွက်ချက်ထည့်သွင်းခြင်း
                        if (model.VariantsDto != null && model.VariantsDto.Any())
                        {
                            var newVariants = model.VariantsDto.Select(v => new ProductVariant
                            {
                                ProductId = newProductId, // အပေါ်မှ ရလာသော ID အစစ်ကို သုံးခြင်း
                                Size = v.Size,
                                Color = v.Color,
                                StockQuantity = v.StockQuantity,
                                // 🟢 Space များကို ဖယ်ထုတ်ပြီး ရှင်းလင်းသော Auto-Generated SKU ဖန်တီးခြင်း
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

                        var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception($"Database Save Failed: {innerMessage}");
                    }
                }
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
            var product = await _db.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

            if (product == null)
                return false;

            // 🔹 Product update
            product.Name = request.Name;
            product.Description = request.Description;
            product.BasePrice = request.BasePrice;
            product.CategoryId = request.CategoryId;

            // 🔹 Image update
            var primaryImg = product.ProductImages
                .FirstOrDefault(i => i.IsPrimary == true);

            if (primaryImg != null)
            {
                primaryImg.ImageUrl = request.ImageUrl;
            }
            else if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                product.ProductImages.Add(new ProductImage
                {
                    ImageUrl = request.ImageUrl,
                    IsPrimary = true
                });
            }

            // 🔹 Variants update
            foreach (var v in request.Variants)
            {
                var dbVariant = product.ProductVariants
                    .FirstOrDefault(x => x.VariantId == v.VariantId);

                if (dbVariant != null)
                {
                    dbVariant.Size = v.Size;
                    dbVariant.Color = v.Color;
                    dbVariant.StockQuantity = v.StockQuantity;
                }
            }

            await _db.SaveChangesAsync();
            return true;
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
