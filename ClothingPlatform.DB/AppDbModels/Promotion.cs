using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingPlatform.DB.AppDbModels;

[Table("promotions")]
public class Promotion
{
    [Key]
    [Column("promo_id")]
    public int PromoId { get; set; }

    [Column("title")]
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = "";

    [Column("subtitle")]
    [StringLength(100)]
    public string Subtitle { get; set; } = "";

    [Column("description")]
    [StringLength(500)]
    public string Description { get; set; } = "";

    [Column("promo_code")]
    [Required]
    [StringLength(50)]
    public string PromoCode { get; set; } = "";

    [Column("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [Column("button_text")]
    [StringLength(100)]
    public string ButtonText { get; set; } = "Shop Now";

    [Column("gradient_css")]
    [StringLength(250)]
    public string GradientCss { get; set; } = "";

    [Column("image_url")]
    [StringLength(500)]
    public string ImageUrl { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
