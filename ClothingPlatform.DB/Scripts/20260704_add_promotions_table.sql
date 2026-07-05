-- Create promotions table and seed data
IF OBJECT_ID('dbo.promotions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.promotions (
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
END
GO

-- Seed promotions
IF NOT EXISTS (SELECT 1 FROM dbo.promotions WHERE promo_code = 'SUMMER20')
BEGIN
    INSERT INTO dbo.promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url)
    VALUES (
        N'Summer Silhouette Sale', 
        N'LIMITED TIME OFFER', 
        N'Embrace the warmth of Yangon in elegance. Get 20% off on all lightweight linen and silk creations.', 
        N'SUMMER20', 
        20.00, 
        N'Claim 20% Discount', 
        N'linear-gradient(135deg, #8B1A1A 0%, #3C1F10 100%)',
        N'https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=1200&q=80'
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.promotions WHERE promo_code = 'LOYAL2X')
BEGIN
    INSERT INTO dbo.promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url)
    VALUES (
        N'Double Atelier Points', 
        N'EXCLUSIVE ROYAL LOYALTY', 
        N'Upgrade your status faster. Earn 2x loyalty points on all orders confirmed this weekend.', 
        N'LOYAL2X', 
        0.00, 
        N'Explore Collection', 
        N'linear-gradient(135deg, #3C1F10 0%, #C9A96E 100%)',
        N'https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&q=80'
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.promotions WHERE promo_code = 'MONSOON10')
BEGIN
    INSERT INTO dbo.promotions (title, subtitle, description, promo_code, discount_percent, button_text, gradient_css, image_url)
    VALUES (
        N'Monsoon Preview Event', 
        N'EARLY ACCESS DISPATCH', 
        N'Get a complimentary matching designer mask and custom resizing on pre-orders.', 
        N'MONSOON10', 
        10.00, 
        N'Unlock Early Access', 
        N'linear-gradient(135deg, #7A5C50 0%, #EDD9D0 100%)',
        N'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=1200&q=80'
    );
END
GO

-- Add promo_id column to products table if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.products') AND name = 'promo_id')
BEGIN
    ALTER TABLE dbo.products ADD promo_id INT NULL;
    ALTER TABLE dbo.products ADD CONSTRAINT FK_products_promotions FOREIGN KEY (promo_id) REFERENCES dbo.promotions(promo_id) ON DELETE SET NULL;

    -- Seed initial product promotion assignments
    -- Assign first 2 products to SUMMER20 (promo_id = 1)
    UPDATE TOP(2) dbo.products SET promo_id = 1 WHERE promo_id IS NULL;
    -- Assign next 2 products to MONSOON10 (promo_id = 3)
    UPDATE TOP(2) dbo.products SET promo_id = 3 WHERE promo_id IS NULL;
END
GO
