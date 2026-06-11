namespace ClothingPlatformProject.Models.Product
{
    public class ProductCreateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
    }

    public class ProductResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        public ProductModel? Data { get; set; }
    }
    public class ProductVariantResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        //public ProductVariantModel? Data { get; set; }
    }
    public class ProductImageResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
       // public ProductImageModel? Data { get; set; }
    }
}
