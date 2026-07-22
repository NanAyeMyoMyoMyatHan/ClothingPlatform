using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Category;

namespace ClothingPlatform.Api.Features.Category
{
    public interface ICategoryService
    {
        List<CategoryDto> GetCategories();
        CategoryDto? GetCategoryById(int id);
        void CreateCategory(CategoryRequestModel model);
        void UpdateCategory(UpdateRequsetModel category,int id);
        void DeleteCategory(int id);
    }

   
}
