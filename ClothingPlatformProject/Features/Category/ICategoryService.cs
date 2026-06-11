using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Category;

namespace ClothingPlatformProject.Features.Category
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
