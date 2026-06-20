using ClothingPlatform.Api.Models.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Category
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
       private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public IActionResult GetCategory()
        {
            var getcat=_categoryService.GetCategories();
            return Ok(getcat);
        }
        [HttpGet("{id}")]
        public IActionResult GetCategory(int id)
        {
            var result = _categoryService.GetCategoryById(id);
            if(result == null)
            {
                return NotFound("There is no Category");
            }
            return Ok(result);
        }
        [HttpPost]
        [Authorize(Policy = "AdminOrStaff")]
        public IActionResult CreateCategory(CategoryRequestModel model)
        {
            _categoryService.CreateCategory(model);
            return Ok("Create Successful");
        }
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrStaff")]
        public IActionResult UpdateCategory(UpdateRequsetModel model,int id)
        {
            _categoryService.UpdateCategory(model,id);
            return Ok("Update Success");
        }
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOrStaff")]
        public IActionResult DeleteCategory(int id)
        {
            _categoryService.DeleteCategory(id);
            return Ok("Delete Successful");
        }
    }
}
