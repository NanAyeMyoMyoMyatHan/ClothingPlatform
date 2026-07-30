using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.User;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

//using ClothingPlatform.Api.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.Product
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
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var newProduct = new ClothingPlatform.DB.AppDbModels.Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId
                };
                _db.Products.Add(newProduct);
                await _db.SaveChangesAsync();

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

                _db.ProductImages.Add(new ProductImage
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
                        SalePrice = v.SalePrice,
                        PurchasePrice = v.PurchasePrice,
                        Sku = $"{model.Name.Replace(" ", "").ToUpper()}-{v.Size.Replace(" ", "").ToUpper()}-{v.Color.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}"
                    }).ToList();

                    _db.ProductVariants.AddRange(newVariants);
                }

                await _db.SaveChangesAsync();

                // Log initial stock as Stock-In Voucher entries so they appear in Stock-In Voucher History
                if (model.StaffId > 0 && model.VariantsDto != null)
                {
                    // Reload the saved variants to get their generated IDs and SKUs
                    var savedVariants = await _db.ProductVariants
                        .Where(v => v.ProductId == newProductId && v.StockQuantity > 0)
                        .ToListAsync();

                    // Use the current user's ID directly as the log's StaffId (works for both Admin and Staff users)
                    int operationalStaffId = model.StaffId;

                    foreach (var variant in savedVariants)
                    {
                        _db.StaffActivityLogs.Add(new StaffActivityLog
                        {
                            StaffId = operationalStaffId,
                            TargetTable = "product_variants",
                            TargetId = variant.VariantId,
                            ActionType = "create",
                            Description = $"Stock-In Voucher: New product '{model.Name}' — SKU {variant.Sku}. Initial stock: {variant.StockQuantity} units. Purchase Price: {variant.PurchasePrice:N0} MMK.",
                            CreatedAt = DateTime.Now
                        });
                    }
                    await _db.SaveChangesAsync();
                }

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
        SalePrice = p.ProductVariants.Any()
            ? p.ProductVariants.Min(v => v.SalePrice ?? 0m)
            : 0m,
        CategoryName = p.Category.Name,
        CategoryId = p.CategoryId,
        VariantsDto = p.ProductVariants.Select(v => new VariantDto
        {
            VariantId = v.VariantId,
            Size = v.Size,
            Color = v.Color,
            StockQuantity = v.StockQuantity,
            SalePrice = v.SalePrice ?? 0m,
            PurchasePrice = v.PurchasePrice
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
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // ၁။ ပြင်မယ့် Product ရှိ၊ မရှိ ID ဖြင့် အရင်ရှာမယ် (ဒီနေရာမှာ model.ProductId ပါလာရပါမယ်)
                var existingProduct = await _db.Products.FindAsync(request.Id);
                if (existingProduct == null)
                {
                    throw new Exception($"Product with ID {request.Id} not found.");
                }

                // ၂။ အချက်အလက်အသစ်များ အစားထိုး ပြင်ဆင်ခြင်း
                existingProduct.Name = request.Name;
                existingProduct.Description = request.Description;
                existingProduct.CategoryId = request.CategoryId;

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

                    var existingImage = _db.ProductImages.FirstOrDefault(i => i.ProductId == request.Id && i.IsPrimary == true);

                    if (existingImage != null)
                    {
                        existingImage.ImageUrl = fileName;
                    }
                    else
                    {
                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = request.Id,
                            ImageUrl = fileName,
                            IsPrimary = true
                        });
                    }
                }

                var oldVariants = _db.ProductVariants
                    .Where(v => v.ProductId == request.Id)
                    .OrderBy(v => v.VariantId)
                    .ToList();

                var requestedVariants = request.VariantsDto ?? new List<VariantDto>();
                var consumedVariantIds = new HashSet<int>();
                var existingVariantsById = oldVariants.ToDictionary(v => v.VariantId);

                foreach (var requestedVariant in requestedVariants.Where(v => v.VariantId > 0))
                {
                    if (!existingVariantsById.TryGetValue(requestedVariant.VariantId, out var existingVariant))
                    {
                        continue;
                    }

                    var safeSize = string.IsNullOrWhiteSpace(requestedVariant.Size) ? "FREE" : requestedVariant.Size.Trim();
                    var safeColor = string.IsNullOrWhiteSpace(requestedVariant.Color) ? "MIX" : requestedVariant.Color.Trim();
                    var safeProdName = string.IsNullOrWhiteSpace(request.Name) ? "PROD" : request.Name.Replace(" ", "");

                    existingVariant.Size = safeSize;
                    existingVariant.Color = safeColor;
                    // Prevent directly editing the existing stock quantity on normal product updates.
                    // Instead, restocking is done via dedicated Stock-In Vouchers.
                    // existingVariant.StockQuantity = Math.Max(0, requestedVariant.StockQuantity);
                    existingVariant.SalePrice = requestedVariant.SalePrice;
                    existingVariant.PurchasePrice = requestedVariant.PurchasePrice;
                    existingVariant.Sku = $"{safeProdName.ToUpper()}-{safeSize.Replace(" ", "").ToUpper()}-{safeColor.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}";

                    consumedVariantIds.Add(existingVariant.VariantId);
                }

                foreach (var existingVariant in oldVariants.Where(v => !consumedVariantIds.Contains(v.VariantId)))
                {
                    if (await VariantHasReferencesAsync(_db, existingVariant.VariantId))
                    {
                        existingVariant.StockQuantity = 0;
                    }
                    else
                    {
                        _db.ProductVariants.Remove(existingVariant);
                    }
                }

                var newVariants = requestedVariants
                    .Where(v => v.VariantId <= 0)
                    .Select(v =>
                    {
                        var safeSize = string.IsNullOrWhiteSpace(v.Size) ? "FREE" : v.Size.Trim();
                        var safeColor = string.IsNullOrWhiteSpace(v.Color) ? "MIX" : v.Color.Trim();
                        var safeProdName = string.IsNullOrWhiteSpace(request.Name) ? "PROD" : request.Name.Replace(" ", "");

                        return new ProductVariant
                        {
                            ProductId = request.Id,
                            Size = safeSize,
                            Color = safeColor,
                            StockQuantity = Math.Max(0, v.StockQuantity),
                            SalePrice = v.SalePrice,
                            PurchasePrice = v.PurchasePrice,
                            Sku = $"{safeProdName.ToUpper()}-{safeSize.Replace(" ", "").ToUpper()}-{safeColor.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}"
                        };
                    })
                    .ToList();

                if (newVariants.Any())
                {
                    _db.ProductVariants.AddRange(newVariants);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true; // အောင်မြင်ရင် true ပြန်မယ်
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Database Update Failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task<bool> VariantHasReferencesAsync(AppDbContext db, int variantId)
        {
            return await db.OrderItems.AnyAsync(oi => oi.VariantId == variantId)
                || await db.GuestOrderItems.AnyAsync(gi => gi.VariantId == variantId)
                || await db.CartItems.AnyAsync(ci => ci.VariantId == variantId)
                || await db.StaffSalesLogs.AnyAsync(sl => sl.VariantId == variantId);
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
            // Step 1: Group OrderItems → get (ProductId, TotalSold).
            // Avoid deep g.First().Variant.Product.* navigation inside GroupBy projections
            // as EF Core's NavigationExpandingExpressionVisitor cannot resolve them correctly.
            var salesQuery = _db.OrderItems
                .AsNoTracking()
                .Where(x => x.Variant != null && x.Variant.ProductId != null && x.Variant.Product.PromoId == null);

            if (categoryId > 0)
                salesQuery = salesQuery.Where(x => x.Variant.Product.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                salesQuery = salesQuery.Where(x =>
                    x.Variant.Product.Name.Contains(search) ||
                    (x.Variant.Product.Description != null &&
                     x.Variant.Product.Description.Contains(search)));

            var salesTotals = await salesQuery
                .GroupBy(x => x.Variant.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var productIds = salesTotals.Select(s => s.ProductId).ToList();
            var totalCount = productIds.Count;

            // Step 2: Fetch product data via a clean Products query (no GroupBy navigation issues).
            var pagedProductIds = salesTotals
                .OrderByDescending(s => s.TotalSold)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.ProductId)
                .ToList();

            var products = await _db.Products
                .AsNoTracking()
                .Where(p => pagedProductIds.Contains(p.ProductId))
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .ToListAsync();

            var salesLookup = salesTotals.ToDictionary(s => s.ProductId, s => s.TotalSold);

            var items = pagedProductIds
                .Select(pid => products.FirstOrDefault(p => p.ProductId == pid))
                .Where(p => p != null)
                .Select(p => new BestSellerDto
                {
                    ProductId = p!.ProductId,
                    Name = p.Name,
                    TotalSold = salesLookup.TryGetValue(p.ProductId, out var ts) ? ts : 0,
                    SalePrice = p.ProductVariants.Any()
                        ? p.ProductVariants.Min(v => v.SalePrice ?? 0m)
                        : 0m,
                    CategoryName = p.Category?.Name ?? "General",
                    Description = p.Description ?? string.Empty,
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
                            StockQuantity = v.StockQuantity,
                            SalePrice = v.SalePrice ?? 0m,
                            PurchasePrice = v.PurchasePrice
                        })
                        .ToList()
                })
                .ToList();

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
                .Where(p => p.PromoId == null)
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
                    SalePrice = p.ProductVariants.Any()
                        ? p.ProductVariants.Min(v => v.SalePrice ?? 0m)
                        : 0m,
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
                            StockQuantity = v.StockQuantity,
                            SalePrice = v.SalePrice ?? 0m,
                            PurchasePrice = v.PurchasePrice
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
                .Where(p => p.PromoId == null)
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
                    SalePrice = p.ProductVariants.Any()
                        ? p.ProductVariants.Min(v => v.SalePrice ?? 0m)
                        : 0m,
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
                            StockQuantity = v.StockQuantity,
                            SalePrice = v.SalePrice ?? 0m,
                            PurchasePrice = v.PurchasePrice
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
