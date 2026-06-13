public class VariantDto
{
    public int VariantId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

// GET All Products အတွက် သုံးမည့် Model
public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
    public List<VariantDto> Variants { get; set; } = new();
    public ProductImageModel Image { get; set; }
}
public class ProductImageModel
{
    public string ImageUrl { get; set; }
}
// GET Product By ID အတွက် DTO
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
}

// POST Request အတွက် Model
public class ProductCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
}

// PUT Request အတွက် Model
public class ProductUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
}