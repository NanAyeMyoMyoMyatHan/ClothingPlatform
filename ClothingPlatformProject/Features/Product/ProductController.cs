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

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var result = _productService.GetProductById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult CreateProduct(ProductCreateRequest model)
        {
            _productService.CreateProduct(model);
            return Ok("Create Success");
        }

        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, ProductUpdateRequest model)
        {
            _productService.UpdateProduct(id, model);
            return Ok("Update Success");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            _productService.DeleteProduct(id);
            return Ok("Delete Successfully");
        }
    }
}
