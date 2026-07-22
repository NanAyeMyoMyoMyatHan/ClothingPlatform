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
    [StringLength(50)]
    public string? PromoCode { get; set; }

    [Column("discount_percent")]
    public decimal DiscountPercent { get; set; }

    [Column("promo_type")]
    [StringLength(50)]
    public string PromoType { get; set; } = "Percent";

    [Column("discount_value")]
    public decimal DiscountValue { get; set; } = 0;

    [Column("button_text")]
    [StringLength(100)]
    public string ButtonText { get; set; } = "Shop Now";

    [Column("gradient_css")]
    [StringLength(250)]
    public string GradientCss { get; set; } = "";

    [Column("image_url")]
    [StringLength(500)]
    public string ImageUrl { get; set; } = "";

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("usage_limit")]
    public int UsageLimit { get; set; } = 0;

    [Column("user_limit")]
    public int UserLimit { get; set; } = 0;

    [Column("redeemed")]
    public int Redeemed { get; set; } = 0;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("apply_all")]
    public bool ApplyAll { get; set; } = true;

    [Column("is_coupon")]
    public bool IsCoupon { get; set; } = false;

    [Column("new_member_only")]
    public bool NewMemberOnly { get; set; } = false;

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
