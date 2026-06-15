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
                                ProductId = newProductId, // အပေါ်မှ ရလာသော ID အစစ်ကို သုံးခြင်း
                                ImageUrl = model.ImageDto.ImageUrl
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

                        // ၄။ Image နှင့် Variants ဒေတာများကိုပါ ဒုတိယအကြိမ် သိမ်းဆည်းခြင်း
                        await context.SaveChangesAsync();

                        // ၅။ ဇယားအားလုံး အောင်မြင်စွာ ဝင်သွားပြီဖြစ်၍ Transaction ကို အတည်ပြု (Commit) လုပ်ခြင်း
                        await transaction.CommitAsync();

                        return newProductId;
                    }
                    catch (Exception ex)
                    {
                        // 🛑 တစ်ခုခု မှားယွင်းခဲ့ပါက ဒေတာအားလုံးကို Rollback လုပ်၍ မူလအတိုင်း ပြန်ဖြစ်စေခြင်း
                        await transaction.RollbackAsync();

                        // 🟢 အကောင်းဆုံး အဆင့်မြှင့်တင်ချက်- တကယ့် SQL Inner Exception Error အစစ်အမှန်ကို ဆွဲထုတ်ပြီး အပြင်သို့ ပစ်ပေးခြင်း
                        var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception($"Database Save Failed: {innerMessage}");
                    }
                }
            }
        }

        public async Task<bool> UpdateStepByStepAsync(ProductModel model)
        {
            using (var context = new AppDbContext())
            {
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // ၁။ မူရင်း Product ရှိ၊ မရှိ Database ထဲတွင် အရင်ရှာပါ
                        var existingProduct = await context.Products
                            .FirstOrDefaultAsync(p => p.ProductId == model.Id); // သင့် PK အတိုင်း ညှိပေးပါ (model.Id သို့မဟုတ် model.ProductId)

                        if (existingProduct == null)
                        {
                            throw new Exception($"Product with ID {model.Id} not found.");
                        }

                        // ၂။ Main Product အချက်အလက်များကို Update လုပ်ခြင်း
                        existingProduct.Name = model.Name;
                        existingProduct.Description = model.Description;
                        existingProduct.CategoryId = model.CategoryId;
                        existingProduct.BasePrice = model.BasePrice;
                        // existingProduct.UpdatedAt = DateTime.Now; // သင့် DB မှာ ပါဝင်ပါက ဖွင့်ပေးနိုင်ပါသည်

                        // ၃။ Image ကို Update လုပ်ခြင်း
                        if (model.ImageDto != null)
                        {
                            // ရှိပြီးသား ပုံ ရှိ၊ မရှိ စစ်ဆေးခြင်း
                            var existingImage = await context.ProductImages
                                .FirstOrDefaultAsync(img => img.ProductId == existingProduct.ProductId);

                            if (existingImage != null)
                            {
                                // ရှိပြီးသားပုံဆိုလျှင် URL အသစ်လဲခြင်း
                                existingImage.ImageUrl = model.ImageDto.ImageUrl;
                            }
                            else
                            {
                                // မူလက ပုံမရှိခဲ့ဖူးပါက အသစ်တစ်ခု Add ပေးခြင်း
                                var newImage = new ProductImage
                                {
                                    ProductId = existingProduct.ProductId,
                                    ImageUrl = model.ImageDto.ImageUrl
                                };
                                context.ProductImages.Add(newImage);
                            }
                        }

                        // ၄။ Variants များကို Sync လုပ်ခြင်း (အဟောင်းများကို ဖျက်၍ အသစ်များ အစားထိုးခြင်း - အရှင်းဆုံးနည်းလမ်း)
                        if (model.VariantsDto != null)
                        {
                            // (က) ဒီ Product နဲ့ ပတ်သက်ပြီး DB ထဲမှာ ရှိနေပြီးသား Variant အဟောင်းတွေကို အရင်လိုက်ရှာပြီး ဖျက်ပစ်ပါမယ်
                            var oldVariants = context.ProductVariants
                                .Where(v => v.ProductId == existingProduct.ProductId);

                            context.ProductVariants.RemoveRange(oldVariants);

                            // (ခ) UI က တက်လာတဲ့ Variants အသစ်တွေကို စုစည်းပြီး အစားထိုး ထည့်သွင်းခြင်း
                            var updatedVariants = model.VariantsDto.Select(v => new ProductVariant
                            {
                                ProductId = existingProduct.ProductId,
                                Size = v.Size,
                                Color = v.Color,
                                StockQuantity = v.StockQuantity,
                                // SKU မပါလာပါက Auto-Regenerate ပြန်လုပ်ပေးခြင်း၊ ပါလာပါက ၎င်းအတိုင်း သုံးခြင်း
                                Sku = string.IsNullOrWhiteSpace(v.Sku)
                                    ? $"{model.Name.Replace(" ", "").ToUpper()}-{v.Size.Replace(" ", "").ToUpper()}-{v.Color.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}"
                                    : v.Sku
                            }).ToList();

                            context.ProductVariants.AddRange(updatedVariants);
                        }

                        // ၅။ အပြောင်းအလဲ အားလုံးကို Database ထဲသို့ Save လုပ်ခြင်း
                        await context.SaveChangesAsync();

                        // ၆။ အားလုံး အောင်မြင်ပါက Transaction အတည်ပြုခြင်း
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // တစ်ခုခု မှားယွင်းပါက မူလအတိုင်း Rollback ပြန်လုပ်ခြင်း
                        await transaction.RollbackAsync();

                        var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        throw new Exception($"Database Update Failed: {innerMessage}");
                    }
                }
            }
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
        public List<ProductModel> GetAllProducts()
        {
            // 💡 အဆင့်မြှင့်ချက် ၁- .Include() တွေ အကုန်ဖြုတ်လိုက်လို့ ကုဒ်လည်းသန့်၊ ပိုလည်း မြန်သွားပါပြီ။
            return _db.Products
                .AsNoTracking()
                .Select(x => new ProductModel
                {
                    Id = x.ProductId, // 💡 ရှေ့မှာပြင်ခဲ့တဲ့အတိုင်း Product ရဲ့ PK က 'Id' ဖြစ်ရင် x.Id လို့ ပြောင်းသုံးပေးပါ
                    Name = x.Name,
                    Description = x.Description,
                    CategoryId = x.CategoryId,
                    BasePrice = x.BasePrice,
                    IsFeatured = true,
                    

                    // 💡 အဆင့်မြှင့်ချက် ၂- _db ကို ထပ်မခေါ်ဘဲ Navigation Property ဖြစ်တဲ့ 'x.ProductVariants' ကို တိုက်ရိုက် သုံးခြင်း
                    VariantsDto = x.ProductVariants.Select(v => new VariantDto
                    {
                        VariantId = v.VariantId, // သင့် Variant Table ရဲ့ PK နာမည်အတိုင်း ညှိပေးပါ (ဥပမာ- v.Id သို့မဟုတ် v.VariantId)
                        Size = v.Size,
                        Color = v.Color,
                        StockQuantity = v.StockQuantity
                    }).ToList(),

                    // 💡 အဆင့်မြှင့်ချက် ၃- Image အတွက်လည်း 'x.ProductImage' (1-to-1) သို့မဟုတ် Navigation လမ်းကြောင်းကို တိုက်ရိုက်သုံးခြင်း
                    // 🟢 ဘယ်ဘက်က Property နာမည်ကို Image ဟု ပြောင်းလဲပြီး ညာဘက်ကို ပြည့်စုံအောင် ရေးခြင်း
                    ImageDto = x.ProductImages != null && x.ProductImages.Any()
                ? new ProductImageModel
                {
                    ImageUrl = x.ProductImages.Select(i => i.ImageUrl).FirstOrDefault() ?? string.Empty
                }
                : null // 💡 တကယ်လို့ Image မရှိရင် null ထည့်ပေးရန်
                })
                .ToList();
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

        public async Task<List<BestSellerDto>> GetAllBestSellersAsync()
        {
            return await _db.OrderItems
        .Include(x => x.Variant)
            .ThenInclude(v => v.Product)
                .ThenInclude(p => p.ProductImages)
        .GroupBy(x => x.Variant.ProductId)
        .Select(g => new BestSellerDto
        {
            ProductId = g.Key,
            Name = g.First().Variant.Product.Name,
            TotalSold = g.Sum(x => x.Quantity),
            BasePrice = g.First().Variant.Product.BasePrice,
            CategoryName = g.First().Variant.Product.Category.Name,
            ImageDto = g.First().Variant.Product.ProductImages
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()

        })
        .OrderByDescending(x => x.TotalSold)
        .ToListAsync();
        }

        public async Task<List<NewCreationDto>> GetAllNewCreationAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .OrderByDescending(p => p.CreatedAt)
                
                .Take(10)// or ProductId if preferred
                
                .Select(p => new NewCreationDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    Description = p.Description ?? string.Empty,
                    CategoryName = p.Category != null
                        ? p.Category.Name
                        : "General",

                    ImageDto = p.ProductImages
                        .Where(i => i.IsPrimary==true)
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
        }
    }
   

}
