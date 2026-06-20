using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Category;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _db;

        public CategoryService(AppDbContext db)
        {
            _db = db;
        }

        public void CreateCategory(CategoryRequestModel model)
        {
            var category = new ClothingPlatform.DB.AppDbModels.Category
            {
                Name = model.Name,
                Slug = model.slugs,
                ParentId = model.parent_id
            };
            _db.Categories.Add(category);
            _db.SaveChanges();
        }

        public void DeleteCategory(int id)
        {
            var item = _db.Categories
                 .AsNoTracking()
                 .FirstOrDefault(x => x.CategoryId == id);
            if (item == null) { return ; }
            _db.Categories.Remove(item);
            _db.SaveChanges();
        }

        public List<CategoryDto> GetCategories()
        {
            var category = _db.Categories.AsNoTracking()
                .Select(x=>new CategoryDto
                {
                    Name = x.Name,
                    slugs = x.Slug,
                    parent_id = x.ParentId
                }).ToList();
            
            return (category);
        }

        public CategoryDto? GetCategoryById(int id)
        {
            var item = _db.Categories
                 .AsNoTracking()
                 .FirstOrDefault(x => x.CategoryId == id);
            if(item == null) { return null; }
            return new CategoryDto
            {
                Id = item.CategoryId,
                Name = item.Name,
                slugs = item.Slug,
                parent_id= item.ParentId
            };
            


        }

        public void UpdateCategory(UpdateRequsetModel category, int id)
        {
            var item = _db.Categories
                 .AsNoTracking()
                 .FirstOrDefault(x => x.CategoryId == id);
            if (item == null) { return ; }
            category.Name = item.Name;
            category.slugs = item.Slug;
            category.parent_id = item.ParentId;
            _db.SaveChanges();
        }
    }
}
