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

        [HttpGet]
        public IActionResult GetAllProducts()
        {
            return Ok(_productService.GetAllProducts());
        }

        [HttpGet("{id:int}")]
        public IActionResult GetProductById(int id)
        {
            var result = _productService.GetProductById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        
      

        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, ProductUpdateRequest model)
        {
            _productService.UpdateProduct(id, model);
            return Ok("Update Success");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);

            if (!result)
                return NotFound();

            return Ok(true);
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
        [HttpGet("bestsellers")] // 🟢 တွန့်ကွင်းဖြုတ်ပြီး နာမည်တိုက်ရိုက်ပေးလိုက်ပါပြီ
        public async Task<ActionResult<List<BestSellerDto>>> GetBestSellers()
        {
            var result = await _productService.GetAllBestSellersAsync();
            return Ok(result); // 💡 ActionResult နှင့် Ok() သုံးပေးခြင်းက ပိုမိုစနစ်ကျပါသည်
        }

        // ၂။ New Creations ဆွဲထုတ်မည့် API (URL လမ်းကြောင်း: api/product/new-creations)
        [HttpGet("new-creations")] // 🟢 တွန့်ကွင်းဖြုတ်ပြီး လမ်းကြောင်းသီးသန့်ခွဲလိုက်ပါပြီ
        public async Task<ActionResult<List<NewCreationDto>>> GetNewCreation()
        {
            var result = await _productService.GetAllNewCreationAsync();
            return Ok(result);
        }
    }
}
