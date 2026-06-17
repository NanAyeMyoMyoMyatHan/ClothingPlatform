public class VariantDto
{
    public int VariantId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal? PriceModifier { get; set; }
}

// GET All Products အတွက် သုံးမည့် Model
public class ProductModel
{
    public int Id { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
    public List<VariantDto> VariantsDto { get; set; } = new();
    public ProductImageModel ImageDto { get; set; }
}
public class ProductImageModel
{
    public string ImageUrl { get; set; }
    public byte IsPrimary { get; set; }
}
// GET Product By ID အတွက် DTO
public class ProductDto
{
    public int Id { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; }
   public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
    public List<VariantDto> VariantsDto { get; set; } = new();
    public string? ImageDto { get; set; }
}


// PUT Request အတွက် Model
public class UpdateProductRequest
{
    public int ProductId { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public int CategoryId { get; set; }

    public string ImageUrl { get; set; } = "";

    public List<UpdateVariantDto> Variants { get; set; } = new();
}

public class UpdateVariantDto
{
    public int VariantId { get; set; }
    public string Size { get; set; } = "";
    public string Color { get; set; } = "";
    public int StockQuantity { get; set; }
}
public class BestSellerDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }= string.Empty;
    public int TotalSold { get; set; }
    public decimal BasePrice { get; set; }
    public string CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public string? ImageDto { get; set; }
    public string Description { get; set; }
    public List<VariantDto> VariantsDto { get; set; } = new();
}

public class NewCreationDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal BasePrice { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public string? ImageDto { get; set; }
    public string? Description { get; set; }
    public List<VariantDto> VariantsDto { get; set; } = new();

}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}