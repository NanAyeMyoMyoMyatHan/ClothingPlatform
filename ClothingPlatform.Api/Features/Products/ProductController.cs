using ClothingPlatform.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Product
{
    [Route("api/[controller]")]
    
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _env;
        public ProductController(IProductService productService, IWebHostEnvironment env)
        {
            _productService = productService;
            _env = env;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // UPDATE product
        [HttpPut]
        [Authorize(Policy = "AdminOrStaff")]
        
        public async Task<IActionResult> Update([FromBody] UpdateProductRequest model)
        {
            Console.WriteLine($"[API DEBUG] Received ProductId: {model?.Id}");

            // ခံစစ်ကို model အလွတ်ဖြစ်ခြင်း တစ်ခုတည်းကိုပဲ အဓိက စစ်လိုက်ပါမယ်
            if (model == null)
            {
                return BadRequest("Product data cannot be null.");
            }

            // တကယ်လို့ model.ProductId က 0 ဖြစ်နေသေးရင် Service ထဲရောက်မှ Model ထဲက အချက်အလက်တွေနဲ့ ထပ်ရှာလို့ရအောင် ခွင့်ပြုပေးလိုက်မယ်
            try
            {
                bool isUpdated = await _productService.UpdateProductAsync(model);

                if (isUpdated)
                {
                    return Ok(new { Message = $"Product '{model.Name}' updated successfully!" });
                }

                return StatusCode(500, "Update operation failed at database level.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"API Error: {ex.Message}");
            }
        }

        [HttpDelete("{productId}")]
        [Authorize(Policy = "AdminOrStaff")]
        
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var result = await _productService.DeleteProductAsync(productId);
            if (!result)
                return NotFound("Product not found.");
            return Ok(true);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrStaff")]
       
        public async Task<IActionResult> SaveProduct([FromBody] ProductModel model)
        {
            // 💡 သင့်ရဲ့ Product Service ထဲက ဒေတာအသစ်ထည့်တဲ့ မက်သဒ်ကို လှမ်းခေါ်ပြီး ID ပြန်ယူခြင်း
            int generatedProductId = await _productService.InsertStepByStepAsync(model);

            // အောင်မြင်ကြောင်း ID နှင့်တကွ ပြန်ပေးခြင်း
            return Ok(new { id = generatedProductId, message = "Product saved successfully!" });
        }
        [HttpGet("bestSeller")]
        public async Task<IActionResult> GetBestSellers(
     int page = 1, int pageSize = 10,
            string? search = null, int categoryId = 0)
        {
            var result = await _productService.GetAllBestSellersAsync(page, pageSize,search,categoryId);
            return Ok(result);
        }

        // ၂။ New Creations ဆွဲထုတ်မည့် API (URL လမ်းကြောင်း: api/product/new-creations)
        [HttpGet("newCreation")]
        public async Task<IActionResult> GetNewCreations(
    int page = 1, int pageSize = 10,
            string? search = null, int categoryId = 0)
        {
            var result = await _productService.GetAllNewCreationAsync(page, pageSize, search, categoryId);
            return Ok(result);
        }

        [HttpGet("allcollection")]
        public async Task<IActionResult> GetAllCollection(
            int page = 1, int pageSize = 10,
            string? search = null, int categoryId = 0)
        {
            var result = await _productService.GetAllProduct(page, pageSize, search, categoryId);
            return Ok(result);
        }

        [HttpPost("upload-image")]
        [Authorize(Policy = "AdminOrStaff")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            // ၁။ File ရှိမရှိနှင့် အလွတ်ဖြစ်နေသလား အရင်စစ်မယ်
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // ၂။ ပုံမှန် Image Format ဟုတ်မဟုတ် စစ်မယ် (Security အတွက်)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid image format. Only JPG, JPEG, PNG, and WEBP are allowed.");

            // ၃။ Folder လမ်းကြောင်း သတ်မှတ်ပြီး မရှိရင် ဆောက်မယ်
            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // ၄။ Path Injection ကာကွယ်ဖို့ File Name သီးသန့်ကိုပဲ ယူပြီး Guid နဲ့ တွဲမယ်
            var safeFileName = Path.GetFileName(file.FileName);
            var fileName = $"{Guid.NewGuid()}_{safeFileName}";
            var filePath = Path.Combine(folder, fileName);

            // ၅။ File ကို Server ပေါ် သိမ်းဆည်းမယ်
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            } // ဒီနေရာမှာ stream က သေချာပေါက် auto-closed ဖြစ်သွားပါပြီ

            // ၆။ Database မှာ သိမ်းဖို့အတွက် File Name သာ return ပြန်မယ်
            return Ok(new { FileName = fileName });
        }
    }
}
