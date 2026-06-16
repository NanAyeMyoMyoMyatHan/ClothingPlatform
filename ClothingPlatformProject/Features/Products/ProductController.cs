using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Product
{
    [Route("api/[controller]")]
    
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
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
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            if (id != request.ProductId)
                return BadRequest("ID mismatch");

            var result = await _productService.UpdateProductAsync(request);

            if (!result)
                return NotFound();

            return Ok(new
            {
                message = "Product updated successfully"
            });
        }


        [HttpPost] // 💡 အနောက်မှာ ဘာမှ ထပ်မထည့်ပါနဲ့။ ဒါဆိုရင် Base URL "api/product" အတိုင်း အလုပ်လုပ်ပါလိမ့်မယ်။
        public async Task<IActionResult> SaveProduct([FromBody] ProductModel model)
        {
            // 💡 သင့်ရဲ့ Product Service ထဲက ဒေတာအသစ်ထည့်တဲ့ မက်သဒ်ကို လှမ်းခေါ်ပြီး ID ပြန်ယူခြင်း
            int generatedProductId = await _productService.InsertStepByStepAsync(model);

            // အောင်မြင်ကြောင်း ID နှင့်တကွ ပြန်ပေးခြင်း
            return Ok(new { id = generatedProductId, message = "Product saved successfully!" });
        }
        // ၁။ Best Sellers ဆွဲထုတ်မည့် API (URL လမ်းကြောင်း: api/product/bestsellers)
        [HttpGet("bestSeller")]
        public async Task<IActionResult> GetBestSellers(
     int page = 1,
     int pageSize = 5)
        {
            var result = await _productService.GetAllBestSellersAsync(page, pageSize);
            return Ok(result);
        }

        // ၂။ New Creations ဆွဲထုတ်မည့် API (URL လမ်းကြောင်း: api/product/new-creations)
        [HttpGet("newCreation")]
        public async Task<IActionResult> GetNewCreations(
    int page = 1,
    int pageSize = 5)
        {
            var result = await _productService.GetAllNewCreationAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("allcollection")]
        public async Task<IActionResult> GetAllCollection(int page = 1,
    int pageSize = 5)
        {
            var result = await _productService.GetAllProduct(page, pageSize);
            return Ok(result);
        }
    }
}
