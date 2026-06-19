using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.DB.AppDbModels;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CustomerNotification> CustomerNotifications { get; set; }

    public virtual DbSet<GuestOrder> GuestOrders { get; set; }

    public virtual DbSet<GuestOrderItem> GuestOrderItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<StaffActivityLog> StaffActivityLogs { get; set; }

    public virtual DbSet<StaffFulfillmentLog> StaffFulfillmentLogs { get; set; }

    public virtual DbSet<StaffSalesDaily> StaffSalesDailies { get; set; }

    public virtual DbSet<StaffSalesLog> StaffSalesLogs { get; set; }

    public virtual DbSet<StaffSalesMonthly> StaffSalesMonthlies { get; set; }

    public virtual DbSet<StoreSalesDaily> StoreSalesDailies { get; set; }

    public virtual DbSet<StoreSalesMonthly> StoreSalesMonthlies { get; set; }

    public virtual DbSet<TblPermission> TblPermissions { get; set; }

    public virtual DbSet<TblRole> TblRoles { get; set; }

    public virtual DbSet<TblRolePermission> TblRolePermissions { get; set; }

    public virtual DbSet<TblUser> TblUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=ClothingPlatformDB;User ID=sa; Password=sasa@123;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__cart_ite__2EF52A27A47B04D1");

            entity.ToTable("cart_items");

            entity.HasIndex(e => new { e.UserId, e.VariantId }, "UX_CartItems_User_Variant").IsUnique();

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.User).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Cart_Users");

            entity.HasOne(d => d.Variant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK_Cart_Variants");
        });

        modelBuilder.Entity<CustomerNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK_customer_notifications");

            entity.ToTable("customer_notifications");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Title)
                .HasMaxLength(120)
                .HasColumnName("title");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_CustomerNotifications_Users");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__categori__D54EE9B49AB8EA43");

            entity.ToTable("categories");

            entity.HasIndex(e => e.Slug, "UQ__categori__32DD1E4CEA146E15").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("slug");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Categories_Parent");
        });

        modelBuilder.Entity<GuestOrder>(entity =>
        {
            entity.HasKey(e => e.GuestOrderId).HasName("PK__guest_or__78B1CA5877E4BD07");

            entity.ToTable("guest_orders");

            entity.Property(e => e.GuestOrderId).HasColumnName("guest_order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(150)
                .HasColumnName("customer_name");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("order_status");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValue("COD");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.TotalQuantity)
                .HasDefaultValue(1)
                .HasColumnName("total_quantity");
        });

        modelBuilder.Entity<GuestOrderItem>(entity =>
        {
            entity.HasKey(e => e.GuestOrderItemId).HasName("PK__guest_or__027A3BE1777300BE");

            entity.ToTable("guest_order_items");

            entity.Property(e => e.GuestOrderItemId).HasColumnName("guest_order_item_id");
            entity.Property(e => e.GuestOrderId).HasColumnName("guest_order_id");
            entity.Property(e => e.PriceAtPurchase)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price_at_purchase");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.GuestOrder).WithMany(p => p.GuestOrderItems)
                .HasForeignKey(d => d.GuestOrderId)
                .HasConstraintName("FK_GuestOrderItems_GuestOrders");

            entity.HasOne(d => d.Variant).WithMany(p => p.GuestOrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GuestOrderItems_ProductVariants");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__orders__4659622916A91316");

            entity.ToTable("orders");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("order_status");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("unpaid")
                .HasColumnName("payment_status");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("shipping_address");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__order_it__3764B6BC7C249A9E");

            entity.ToTable("order_items");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PriceAtPurchase)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_at_purchase");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Variants");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__payments__ED1FC9EA9BBECDAA");

            entity.ToTable("payments");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage)
                .HasColumnType("text")
                .HasColumnName("error_message");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.SlipImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("slip_image_url");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("transaction_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF5F7C63A06");

            entity.ToTable("products");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsFeatured)
                .HasDefaultValue(false)
                .HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("name");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__product___DC9AC9559F26F083");

            entity.ToTable("product_images");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Images_Products");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("PK__product___EACC68B7899A808F");

            entity.ToTable("product_variants");

            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("color");
            entity.Property(e => e.PriceModifier)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_modifier");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Size)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("size");
            entity.Property(e => e.Sku)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Variants_Products");
        });

        modelBuilder.Entity<StaffActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__staff_ac__9E2397E0A262F058");

            entity.ToTable("staff_activity_logs");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("action_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetTable)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("target_table");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffActivityLogs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activity_Staff");
        });

        modelBuilder.Entity<StaffFulfillmentLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__staff_fu__9E2397E041CAA775");

            entity.ToTable("staff_fulfillment_log");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActionAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("action_at");
            entity.Property(e => e.ActionTaken)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action_taken");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.HasOne(d => d.Order).WithMany(p => p.StaffFulfillmentLogs)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fulfillment_Orders");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffFulfillmentLogs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fulfillment_Staff");
        });

        modelBuilder.Entity<StaffSalesDaily>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__staff_sa__779B7C58CC9B5956");

            entity.ToTable("staff_sales_daily");

            entity.HasIndex(e => new { e.StaffId, e.ReportDate }, "UC_StaffDaily").IsUnique();

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.ReportDate).HasColumnName("report_date");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TotalProductsSold).HasColumnName("total_products_sold");
            entity.Property(e => e.TotalSalesValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_sales_value");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffSalesDailies)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffDaily_Staff");
        });

        modelBuilder.Entity<StaffSalesLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__staff_sa__9E2397E086E6445E");

            entity.ToTable("staff_sales_logs");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.QuantitySold).HasColumnName("quantity_sold");
            entity.Property(e => e.SaleAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("sale_amount");
            entity.Property(e => e.SoldAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("sold_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Order).WithMany(p => p.StaffSalesLogs)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesLogs_Orders");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffSalesLogs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesLogs_Staff");

            entity.HasOne(d => d.Variant).WithMany(p => p.StaffSalesLogs)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesLogs_Variants");
        });

        modelBuilder.Entity<StaffSalesMonthly>(entity =>
        {
            entity.HasKey(e => e.MonthlyReportId).HasName("PK__staff_sa__7FC773875FE5E151");

            entity.ToTable("staff_sales_monthly");

            entity.HasIndex(e => new { e.StaffId, e.ReportYear, e.ReportMonth }, "UC_StaffMonthly").IsUnique();

            entity.Property(e => e.MonthlyReportId).HasColumnName("monthly_report_id");
            entity.Property(e => e.ReportMonth).HasColumnName("report_month");
            entity.Property(e => e.ReportYear).HasColumnName("report_year");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TotalProductsSold).HasColumnName("total_products_sold");
            entity.Property(e => e.TotalSalesValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_sales_value");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffSalesMonthlies)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffMonthly_Staff");
        });

        modelBuilder.Entity<StoreSalesDaily>(entity =>
        {
            entity.HasKey(e => e.DailySummaryId).HasName("PK__store_sa__D4ACC356C87F0811");

            entity.ToTable("store_sales_daily");

            entity.HasIndex(e => e.ReportDate, "UQ__store_sa__7BFFBECF8DA84555").IsUnique();

            entity.Property(e => e.DailySummaryId).HasColumnName("daily_summary_id");
            entity.Property(e => e.ActiveStaffCount).HasColumnName("active_staff_count");
            entity.Property(e => e.ReportDate).HasColumnName("report_date");
            entity.Property(e => e.TotalProductsSold).HasColumnName("total_products_sold");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_revenue");
        });

        modelBuilder.Entity<StoreSalesMonthly>(entity =>
        {
            entity.HasKey(e => e.MonthlySummaryId).HasName("PK__store_sa__6AFCE4A8ABC6DEE9");

            entity.ToTable("store_sales_monthly");

            entity.HasIndex(e => new { e.ReportYear, e.ReportMonth }, "UC_StoreMonthly").IsUnique();

            entity.Property(e => e.MonthlySummaryId).HasColumnName("monthly_summary_id");
            entity.Property(e => e.ReportMonth).HasColumnName("report_month");
            entity.Property(e => e.ReportYear).HasColumnName("report_year");
            entity.Property(e => e.TotalProductsSold).HasColumnName("total_products_sold");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_revenue");
        });

        modelBuilder.Entity<TblPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__Tbl_Perm__EFA6FB2F8202D7D7");

            entity.ToTable("Tbl_Permissions");

            entity.HasIndex(e => e.PermissionName, "UQ__Tbl_Perm__0FFDA35752B34B95").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.PermissionName).HasMaxLength(100);
        });

        modelBuilder.Entity<TblRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Tbl_Role__8AFACE1A112BE116");

            entity.ToTable("Tbl_Roles");

            entity.HasIndex(e => e.RoleName, "UQ__Tbl_Role__8A2B616085D3A83C").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<TblRolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId }).HasName("PK__Tbl_Role__6400A1A889303A12");

            entity.ToTable("Tbl_RolePermissions");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Permission).WithMany(p => p.TblRolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("FK_RolePermissions_Permissions");

            entity.HasOne(d => d.Role).WithMany(p => p.TblRolePermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_RolePermissions_Roles");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Tbl_User__1788CC4C1949D101");

            entity.ToTable("Tbl_Users");

            entity.HasIndex(e => e.Email, "UQ__Tbl_User__A9D10534B5681EFC").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.TblUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F40808BE1");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E61649B16A59D").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("customer")
                .HasColumnName("role");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__wishlist__6151514EDCD8BD54");

            entity.ToTable("wishlists");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Wishlist_Products");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Wishlist_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
