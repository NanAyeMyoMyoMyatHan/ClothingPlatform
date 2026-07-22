using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.DB;

public static class SchemaCompatibility
{
    public static async Task EnsureCancelledOrderStatusSupportAsync(AppDbModels.AppDbContext db, CancellationToken cancellationToken = default)
    {
        // 1. Core database schema checks (orders, returns, contact messages, basic promotions)
        const string coreSql = """
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
            END;
            """;

        await db.Database.ExecuteSqlRawAsync(coreSql, cancellationToken);

        // 2. Ensure new columns are added to the promotions table (if they don't exist yet)
        const string alterPromotionsSql = """
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('promotions') AND name = 'start_date')
            BEGIN
                ALTER TABLE promotions ADD start_date DATETIME NULL;
                ALTER TABLE promotions ADD end_date DATETIME NULL;
                ALTER TABLE promotions ADD usage_limit INT NOT NULL DEFAULT 0;
                ALTER TABLE promotions ADD redeemed INT NOT NULL DEFAULT 0;
                ALTER TABLE promotions ADD enabled BIT NOT NULL DEFAULT 1;
                ALTER TABLE promotions ADD apply_all BIT NOT NULL DEFAULT 1;
                ALTER TABLE promotions ADD note NVARCHAR(500) NULL;
            END;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('promotions') AND name = 'promo_type')
            BEGIN
                ALTER TABLE promotions ADD promo_type NVARCHAR(50) NOT NULL DEFAULT 'Percent';
                ALTER TABLE promotions ADD discount_value DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                EXEC('UPDATE promotions SET promo_type = ''Percent'', discount_value = discount_percent;');
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(alterPromotionsSql, cancellationToken);

        // 3. Ensure seeded data exists (using dynamic SQL EXEC to prevent compiler issues on missing columns in clean schemas)
        const string seedPromotionsSql = """
            IF NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'SUMMER20')
            BEGIN
                EXEC('INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
                VALUES 
                (N''Summer Silhouette Sale'', N''LIMITED TIME OFFER'', N''Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.'', N''SUMMER20'', 20.00, N''Claim 20% Discount'', N''linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)'', N''https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80'', ''2026-06-01'', 1, 1)');
            END;
            IF NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'LOYAL2X')
            BEGIN
                EXEC('INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
                VALUES 
                (N''Double Atelier Points'', N''EXCLUSIVE ROYAL LOYALTY'', N''Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.'', N''LOYAL2X'', 0.00, N''Explore Collection'', N''linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)'', N''https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80'', ''2026-06-01'', 1, 1)');
            END;
            IF NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'MONSOON10')
            BEGIN
                EXEC('INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
                VALUES 
                (N''Monsoon Preview Event'', N''EARLY ACCESS DISPATCH'', N''Get a complimentary matching designer mask and custom resizing on pre-orders.'', N''MONSOON10'', 10.00, N''Unlock Early Access'', N''linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)'', N''https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80'', ''2026-06-01'', 1, 1)');
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(seedPromotionsSql, cancellationToken);

        // 4. Products-Promotions foreign keys
        const string productRelationSql = """
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
        await db.Database.ExecuteSqlRawAsync(productRelationSql, cancellationToken);

        // 5. Add user_limit and is_coupon to promotions table, and applied_promo to orders table
        const string limitsSql = """
            -- Drop UNIQUE constraint if it exists to allow multiple banners to not have a code, or have null codes
            IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ__promotio__F9EC6DC7440409F3' AND object_id = OBJECT_ID('promotions'))
            BEGIN
                ALTER TABLE promotions DROP CONSTRAINT UQ__promotio__F9EC6DC7440409F3;
            END;
            -- Also drop UQ__promotio__F9EC6DC778BA31EE (alternate generated name) or any other unique constraints
            WHILE EXISTS (SELECT * FROM sys.objects WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('promotions'))
            BEGIN
                DECLARE @ConstraintName NVARCHAR(200);
                SELECT TOP 1 @ConstraintName = name FROM sys.objects WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('promotions');
                IF @ConstraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE promotions DROP CONSTRAINT ' + @ConstraintName);
                END;
            END;

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('promotions') AND name = 'user_limit')
            BEGIN
                ALTER TABLE promotions ADD user_limit INT NOT NULL DEFAULT 0;
            END;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('orders') AND name = 'applied_promo')
            BEGIN
                ALTER TABLE orders ADD applied_promo VARCHAR(100) NULL;
            END;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('promotions') AND name = 'is_coupon')
            BEGIN
                ALTER TABLE promotions ADD is_coupon BIT NOT NULL DEFAULT 0;
            END;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('promotions') AND name = 'new_member_only')
            BEGIN
                ALTER TABLE promotions ADD new_member_only BIT NOT NULL DEFAULT 0;
            END;

            IF NOT EXISTS (SELECT 1 FROM promotions WHERE is_coupon = 0)
            BEGIN
                -- Convert all existing promotions to coupons
                EXEC('UPDATE promotions SET is_coupon = 1;');
                
                -- Create separate campaign banners linking to them
                EXEC('INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all, is_coupon)
                      VALUES 
                      (N''Summer Silhouette Sale'', N''LIMITED TIME OFFER'', N''Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.'', NULL, 20.00, N''Claim 20% Discount'', N''linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)'', N''https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80'', ''2026-06-01'', 1, 1, 0),
                      (N''Double Atelier Points'', N''EXCLUSIVE ROYAL LOYALTY'', N''Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.'', NULL, 0.00, N''Explore Collection'', N''linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)'', N''https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80'', ''2026-06-01'', 1, 1, 0),
                      (N''Monsoon Preview Event'', N''EARLY ACCESS DISPATCH'', N''Get a complimentary matching designer mask and custom resizing on pre-orders.'', NULL, 10.00, N''Unlock Early Access'', N''linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)'', N''https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80'', ''2026-06-01'', 1, 1, 0);');

                -- Link products to new campaign banners instead of coupons
                EXEC('
                    DECLARE @SummerBannerId INT;
                    SELECT TOP 1 @SummerBannerId = promo_id FROM promotions WHERE title = N''Summer Silhouette Sale'' AND is_coupon = 0;
                    IF @SummerBannerId IS NOT NULL
                    BEGIN
                        UPDATE products SET promo_id = @SummerBannerId WHERE promo_id = 1;
                    END;

                    DECLARE @MonsoonBannerId INT;
                    SELECT TOP 1 @MonsoonBannerId = promo_id FROM promotions WHERE title = N''Monsoon Preview Event'' AND is_coupon = 0;
                    IF @MonsoonBannerId IS NOT NULL
                    BEGIN
                        UPDATE products SET promo_id = @MonsoonBannerId WHERE promo_id = 3;
                    END;
                ');
            END;
            EXEC('UPDATE promotions SET apply_all = 1 WHERE is_coupon = 1;');
            ALTER TABLE promotions ALTER COLUMN promo_code NVARCHAR(50) NULL;
            """;
        await db.Database.ExecuteSqlRawAsync(limitsSql, cancellationToken);
    }
}
