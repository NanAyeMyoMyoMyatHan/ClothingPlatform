using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.DB;

public static class SchemaCompatibility
{
    public static Task EnsureCancelledOrderStatusSupportAsync(AppDbModels.AppDbContext db, CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_OrderStatus')
            BEGIN
                DECLARE @definition nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.CHK_OrderStatus'));

                IF @definition IS NULL OR @definition NOT LIKE '%CancelledByCustomer%' OR @definition NOT LIKE '%CancelledByStaff%'
                BEGIN
                    ALTER TABLE dbo.orders DROP CONSTRAINT CHK_OrderStatus;
                    ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CHK_OrderStatus
                    CHECK (order_status IN ('Pending', 'Processing', 'Confirm', 'Cancelled', 'CancelledByCustomer', 'CancelledByStaff'));
                END
            END
            ELSE
            BEGIN
                ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CHK_OrderStatus
                CHECK (order_status IN ('Pending', 'Processing', 'Confirm', 'Cancelled', 'CancelledByCustomer', 'CancelledByStaff'));
            END;

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='order_returns' AND xtype='U')
            BEGIN
                CREATE TABLE order_returns (
                    order_return_id INT IDENTITY(1,1) PRIMARY KEY,
                    order_id INT NOT NULL CONSTRAINT FK_OrderReturns_Orders FOREIGN KEY REFERENCES orders(order_id) ON DELETE CASCADE,
                    variant_id INT NOT NULL CONSTRAINT FK_OrderReturns_Variants FOREIGN KEY REFERENCES product_variants(variant_id) ON DELETE CASCADE,
                    quantity INT NOT NULL,
                    reason_checkbox NVARCHAR(255) NOT NULL,
                    reason_text NVARCHAR(MAX) NULL,
                    receipt_image_url NVARCHAR(500) NULL,
                    return_option NVARCHAR(50) NOT NULL,
                    status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    created_at DATETIME NOT NULL DEFAULT GETDATE()
                );
            END
            ELSE
            BEGIN
                IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'status' AND Object_ID = Object_ID(N'order_returns'))
                BEGIN
                    ALTER TABLE dbo.order_returns ADD status NVARCHAR(50) NOT NULL DEFAULT 'Pending';
                END;
            END;

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='contact_messages' AND xtype='U')
            BEGIN
                CREATE TABLE contact_messages (
                    contact_message_id INT IDENTITY(1,1) PRIMARY KEY,
                    full_name NVARCHAR(255) NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    message NVARCHAR(MAX) NOT NULL,
                    created_at DATETIME NOT NULL DEFAULT GETDATE()
                );
            END;

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='promotions' AND xtype='U')
            BEGIN
                CREATE TABLE promotions (
                    promo_id INT IDENTITY(1,1) PRIMARY KEY,
                    title NVARCHAR(150) NOT NULL,
                    subtitle NVARCHAR(100) NULL,
                    description NVARCHAR(500) NULL,
                    promo_code NVARCHAR(50) NOT NULL UNIQUE,
                    discount_percent DECIMAL(5,2) DEFAULT 0.00,
                    button_text NVARCHAR(100) DEFAULT 'Shop Now',
                    gradient_css NVARCHAR(250) NULL,
                    image_url NVARCHAR(500) NULL,
                    created_at DATETIME DEFAULT GETDATE()
                );

                -- Seed original promotions
                INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url)
                VALUES 
                (N'Summer Silhouette Sale', N'LIMITED TIME OFFER', N'Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.', N'SUMMER20', 20.00, N'Claim 20% Discount', N'linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)', N'https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80'),
                (N'Double Atelier Points', N'EXCLUSIVE ROYAL LOYALTY', N'Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.', N'LOYAL2X', 0.00, N'Explore Collection', N'linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)', N'https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80'),
                (N'Monsoon Preview Event', N'EARLY ACCESS DISPATCH', N'Get a complimentary matching designer mask and custom resizing on pre-orders.', N'MONSOON10', 10.00, N'Unlock Early Access', N'linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)', N'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80');
            END;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('products') AND name = 'promo_id')
            BEGIN
                ALTER TABLE products ADD promo_id INT NULL;
                ALTER TABLE products ADD CONSTRAINT FK_products_promotions FOREIGN KEY (promo_id) REFERENCES promotions(promo_id) ON DELETE SET NULL;

                -- Auto-assign first 2 products to SUMMER20 (promo_id = 1) using dynamic SQL to prevent parser errors
                EXEC('UPDATE TOP(2) products SET promo_id = 1 WHERE promo_id IS NULL;');
                -- Auto-assign next 2 products to MONSOON10 (promo_id = 3) using dynamic SQL
                EXEC('UPDATE TOP(2) products SET promo_id = 3 WHERE promo_id IS NULL;');
            END;
            """;

        return db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
