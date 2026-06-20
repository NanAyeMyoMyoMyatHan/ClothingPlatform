namespace ClothingPlatform.Api.Models.Category
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string slugs { get; set; }
        public int? parent_id { get; set; }
    }
    public class CategoryRequestModel
    {
        public string Name { get; set; }
        public string slugs { get; set; }
        public int? parent_id { get; set; }
    }
    public class UpdateRequsetModel
    {
        public string Name { get; set; }
        public string slugs { get; set; }
        public int? parent_id { get; set; }
    }
}
