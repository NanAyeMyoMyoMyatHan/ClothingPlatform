using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.DB;

public static class SchemaCompatibility
{
    public static async Task EnsureCancelledOrderStatusSupportAsync(AppDbModels.AppDbContext db, CancellationToken cancellationToken = default)
    {
        // 1. Core schema: CHK_OrderStatus constraint, order_returns, contact_messages, promotions
        const string coreSql = """
            DO $$
            BEGIN
                -- Update or add CHK_OrderStatus constraint
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_orderstatus') THEN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'chk_orderstatus'
                        AND pg_get_constraintdef(oid) LIKE '%CancelledByCustomer%'
                    ) THEN
                        ALTER TABLE orders DROP CONSTRAINT IF EXISTS chk_orderstatus;
                        ALTER TABLE orders ADD CONSTRAINT chk_orderstatus
                            CHECK (order_status IN ('Pending','Processing','Confirm','Cancelled','CancelledByCustomer','CancelledByStaff'));
                    END IF;
                ELSE
                    BEGIN
                        ALTER TABLE orders ADD CONSTRAINT chk_orderstatus
                            CHECK (order_status IN ('Pending','Processing','Confirm','Cancelled','CancelledByCustomer','CancelledByStaff'));
                    EXCEPTION WHEN duplicate_object THEN NULL;
                    END;
                END IF;

                -- Ensure guest_orders columns match Entity Framework mapping
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'guest_orders' AND column_name = 'paymentmethod') THEN
                    ALTER TABLE guest_orders RENAME COLUMN paymentmethod TO payment_method;
                END IF;
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'guest_orders' AND column_name = 'paymentstatus') THEN
                    ALTER TABLE guest_orders RENAME COLUMN paymentstatus TO payment_status;
                END IF;

                -- Ensure products has is_deleted column for soft delete support
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'products' AND column_name = 'is_deleted') THEN
                    ALTER TABLE products ADD COLUMN is_deleted boolean NOT NULL DEFAULT false;
                END IF;

                -- Automatically synchronize primary key sequences with MAX(id) across all tables
                PERFORM setval('users_user_id_seq', COALESCE((SELECT MAX(user_id) FROM users), 1));
                PERFORM setval('roles_role_id_seq', COALESCE((SELECT MAX(role_id) FROM roles), 1));
                PERFORM setval('products_product_id_seq', COALESCE((SELECT MAX(product_id) FROM products), 1));
                PERFORM setval('product_variants_variant_id_seq', COALESCE((SELECT MAX(variant_id) FROM product_variants), 1));
                PERFORM setval('product_images_image_id_seq', COALESCE((SELECT MAX(image_id) FROM product_images), 1));
                PERFORM setval('categories_category_id_seq', COALESCE((SELECT MAX(category_id) FROM categories), 1));
                PERFORM setval('orders_order_id_seq', COALESCE((SELECT MAX(order_id) FROM orders), 1));
                PERFORM setval('order_items_order_item_id_seq', COALESCE((SELECT MAX(order_item_id) FROM order_items), 1));
                PERFORM setval('guest_orders_guest_order_id_seq', COALESCE((SELECT MAX(guest_order_id) FROM guest_orders), 1));
                PERFORM setval('guest_order_items_guest_order_item_id_seq', COALESCE((SELECT MAX(guest_order_item_id) FROM guest_order_items), 1));
                PERFORM setval('promotions_promo_id_seq', COALESCE((SELECT MAX(promo_id) FROM promotions), 1));
                PERFORM setval('cart_items_cart_id_seq', COALESCE((SELECT MAX(cart_id) FROM cart_items), 1));
                PERFORM setval('payments_payment_id_seq', COALESCE((SELECT MAX(payment_id) FROM payments), 1));
                PERFORM setval('order_returns_order_return_id_seq', COALESCE((SELECT MAX(order_return_id) FROM order_returns), 1));
                PERFORM setval('contact_messages_contact_message_id_seq', COALESCE((SELECT MAX(contact_message_id) FROM contact_messages), 1));
                PERFORM setval('staff_activity_logs_log_id_seq', COALESCE((SELECT MAX(log_id) FROM staff_activity_logs), 1));
                PERFORM setval('customer_notifications_notification_id_seq', COALESCE((SELECT MAX(notification_id) FROM customer_notifications), 1));
            END $$;

            -- order_returns table
            CREATE TABLE IF NOT EXISTS order_returns (
                order_return_id SERIAL PRIMARY KEY,
                order_id INT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
                variant_id INT NOT NULL REFERENCES product_variants(variant_id) ON DELETE CASCADE,
                quantity INT NOT NULL,
                reason_checkbox VARCHAR(255) NOT NULL,
                reason_text TEXT NULL,
                receipt_image_url VARCHAR(500) NULL,
                return_option VARCHAR(50) NOT NULL,
                status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'order_returns' AND column_name = 'status') THEN
                    ALTER TABLE order_returns ADD COLUMN status VARCHAR(50) NOT NULL DEFAULT 'Pending';
                END IF;
            END $$;

            -- contact_messages table
            CREATE TABLE IF NOT EXISTS contact_messages (
                contact_message_id SERIAL PRIMARY KEY,
                full_name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                message TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- promotions table
            CREATE TABLE IF NOT EXISTS promotions (
                promo_id SERIAL PRIMARY KEY,
                title VARCHAR(150) NOT NULL,
                subtitle VARCHAR(100) NULL,
                description VARCHAR(500) NULL,
                promo_code VARCHAR(50) NULL UNIQUE,
                discount_percent DECIMAL(5,2) DEFAULT 0.00,
                button_text VARCHAR(100) DEFAULT 'Shop Now',
                gradient_css VARCHAR(250) NULL,
                image_url VARCHAR(500) NULL,
                created_at TIMESTAMP DEFAULT NOW()
            );
            """;

        await db.Database.ExecuteSqlRawAsync(coreSql, cancellationToken);

        // 2. Add new columns to promotions if they don't exist
        const string alterPromotionsSql = """
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='promotions' AND column_name='start_date') THEN
                    ALTER TABLE promotions ADD COLUMN start_date TIMESTAMP NULL;
                    ALTER TABLE promotions ADD COLUMN end_date TIMESTAMP NULL;
                    ALTER TABLE promotions ADD COLUMN usage_limit INT NOT NULL DEFAULT 0;
                    ALTER TABLE promotions ADD COLUMN redeemed INT NOT NULL DEFAULT 0;
                    ALTER TABLE promotions ADD COLUMN enabled BOOLEAN NOT NULL DEFAULT TRUE;
                    ALTER TABLE promotions ADD COLUMN apply_all BOOLEAN NOT NULL DEFAULT TRUE;
                    ALTER TABLE promotions ADD COLUMN note VARCHAR(500) NULL;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='promotions' AND column_name='promo_type') THEN
                    ALTER TABLE promotions ADD COLUMN promo_type VARCHAR(50) NOT NULL DEFAULT 'Percent';
                    ALTER TABLE promotions ADD COLUMN discount_value DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                    UPDATE promotions SET promo_type = 'Percent', discount_value = discount_percent;
                END IF;
            END $$;
            """;
        await db.Database.ExecuteSqlRawAsync(alterPromotionsSql, cancellationToken);

        // 3. Seed promotions data
        const string seedPromotionsSql = """
            INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
            SELECT 'Summer Silhouette Sale','LIMITED TIME OFFER','Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.','SUMMER20',20.00,'Claim 20% Discount','linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)','https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80','2026-06-01',TRUE,TRUE
            WHERE NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'SUMMER20');

            INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
            SELECT 'Double Atelier Points','EXCLUSIVE ROYAL LOYALTY','Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.','LOYAL2X',0.00,'Explore Collection','linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)','https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80','2026-06-01',TRUE,TRUE
            WHERE NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'LOYAL2X');

            INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all)
            SELECT 'Monsoon Preview Event','EARLY ACCESS DISPATCH','Get a complimentary matching designer mask and custom resizing on pre-orders.','MONSOON10',10.00,'Unlock Early Access','linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)','https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80','2026-06-01',TRUE,TRUE
            WHERE NOT EXISTS (SELECT 1 FROM promotions WHERE promo_code = 'MONSOON10');
            """;
        await db.Database.ExecuteSqlRawAsync(seedPromotionsSql, cancellationToken);

        // 4. Products-Promotions foreign key
        const string productRelationSql = """
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='products' AND column_name='promo_id') THEN
                    ALTER TABLE products ADD COLUMN promo_id INT NULL;
                    ALTER TABLE products ADD CONSTRAINT fk_products_promotions FOREIGN KEY (promo_id) REFERENCES promotions(promo_id) ON DELETE SET NULL;

                    UPDATE products SET promo_id = 1
                    WHERE product_id IN (SELECT product_id FROM products WHERE promo_id IS NULL ORDER BY product_id LIMIT 2);

                    UPDATE products SET promo_id = 3
                    WHERE product_id IN (SELECT product_id FROM products WHERE promo_id IS NULL ORDER BY product_id LIMIT 2);
                END IF;
            END $$;
            """;
        await db.Database.ExecuteSqlRawAsync(productRelationSql, cancellationToken);

        // 5. Add user_limit, is_coupon, new_member_only, applied_promo columns
        const string limitsSql = """
            DO $$
            DECLARE
                v_constraint_name TEXT;
            BEGIN
                -- Drop unique constraints on promotions.promo_code if any exist
                FOR v_constraint_name IN
                    SELECT con.conname FROM pg_constraint con
                    JOIN pg_class cls ON con.conrelid = cls.oid
                    WHERE cls.relname = 'promotions' AND con.contype = 'u'
                LOOP
                    EXECUTE 'ALTER TABLE promotions DROP CONSTRAINT IF EXISTS ' || quote_ident(v_constraint_name);
                END LOOP;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='promotions' AND column_name='user_limit') THEN
                    ALTER TABLE promotions ADD COLUMN user_limit INT NOT NULL DEFAULT 0;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='applied_promo') THEN
                    ALTER TABLE orders ADD COLUMN applied_promo VARCHAR(100) NULL;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='promotions' AND column_name='is_coupon') THEN
                    ALTER TABLE promotions ADD COLUMN is_coupon BOOLEAN NOT NULL DEFAULT FALSE;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='promotions' AND column_name='new_member_only') THEN
                    ALTER TABLE promotions ADD COLUMN new_member_only BOOLEAN NOT NULL DEFAULT FALSE;
                END IF;
            END $$;

            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM promotions WHERE is_coupon = FALSE) THEN
                    UPDATE promotions SET is_coupon = TRUE;

                    INSERT INTO promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url, start_date, enabled, apply_all, is_coupon)
                    VALUES
                    ('Summer Silhouette Sale','LIMITED TIME OFFER','Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.',NULL,20.00,'Claim 20% Discount','linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)','https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80','2026-06-01',TRUE,TRUE,FALSE),
                    ('Double Atelier Points','EXCLUSIVE ROYAL LOYALTY','Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.',NULL,0.00,'Explore Collection','linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)','https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80','2026-06-01',TRUE,TRUE,FALSE),
                    ('Monsoon Preview Event','EARLY ACCESS DISPATCH','Get a complimentary matching designer mask and custom resizing on pre-orders.',NULL,10.00,'Unlock Early Access','linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)','https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80','2026-06-01',TRUE,TRUE,FALSE);

                    -- Link products to new campaign banners
                    UPDATE products SET promo_id = (
                        SELECT promo_id FROM promotions WHERE title = 'Summer Silhouette Sale' AND is_coupon = FALSE LIMIT 1
                    ) WHERE promo_id = 1;

                    UPDATE products SET promo_id = (
                        SELECT promo_id FROM promotions WHERE title = 'Monsoon Preview Event' AND is_coupon = FALSE LIMIT 1
                    ) WHERE promo_id = 3;
                END IF;

                UPDATE promotions SET apply_all = TRUE WHERE is_coupon = TRUE;

                -- Make promo_code nullable
                ALTER TABLE promotions ALTER COLUMN promo_code DROP NOT NULL;
            EXCEPTION WHEN OTHERS THEN NULL;
            END $$;
            """;
        await db.Database.ExecuteSqlRawAsync(limitsSql, cancellationToken);

        // 6. Widen image_url to TEXT so base64 data URLs fit
        const string imageUrlFixSql = """
            ALTER TABLE product_images ALTER COLUMN image_url TYPE text;
            """;
        try { await db.Database.ExecuteSqlRawAsync(imageUrlFixSql, cancellationToken); }
        catch { /* already text — safe to ignore */ }
    }
}
