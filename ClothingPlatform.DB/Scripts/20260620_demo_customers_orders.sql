SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @CustomerRoleId int;
    DECLARE @CustomerPasswordHash varchar(255) = '$2a$11$h.xXrFfW3t8r0IvsJVfp5unXq4CxI0h9t.2hwdzCdXDhbqjpSLEOm';
    DECLARE @StartDate datetime2(0) = '2026-03-20T09:00:00';
    DECLARE @EndDate datetime2(0) = '2026-06-20T21:30:00';

    SELECT @CustomerRoleId = role_id
    FROM dbo.roles
    WHERE LOWER(role_name) = 'customer';

    IF @CustomerRoleId IS NULL
    BEGIN
        INSERT INTO dbo.roles (role_name, description, created_at)
        VALUES ('customer', 'Customer shopping account', SYSDATETIME());

        SET @CustomerRoleId = SCOPE_IDENTITY();
    END;

    DECLARE @SeedCustomers table
    (
        seed_no int NOT NULL PRIMARY KEY,
        first_name varchar(50) NOT NULL,
        last_name varchar(50) NOT NULL,
        email varchar(100) NOT NULL,
        phone_number varchar(100) NOT NULL,
        address varchar(255) NOT NULL
    );

    INSERT INTO @SeedCustomers (seed_no, first_name, last_name, email, phone_number, address)
    VALUES
        (1, 'Aye', 'Chan', 'aye.chan@demo.chic', '09970001001', 'No. 12, Inya Road, Kamayut, Yangon'),
        (2, 'Mya', 'Thandar', 'mya.thandar@demo.chic', '09970001002', 'No. 48, Kabar Aye Pagoda Road, Bahan, Yangon'),
        (3, 'Nilar', 'Win', 'nilar.win@demo.chic', '09970001003', 'No. 73, Pyay Road, Sanchaung, Yangon'),
        (4, 'Hnin', 'Su', 'hnin.su@demo.chic', '09970001004', 'No. 21, Merchant Street, Kyauktada, Yangon'),
        (5, 'Ei', 'Mon', 'ei.mon@demo.chic', '09970001005', 'No. 6, Strand Road, Ahlone, Yangon'),
        (6, 'May', 'Thu', 'may.thu@demo.chic', '09970001006', 'No. 88, University Avenue, Kamayut, Yangon'),
        (7, 'Thiri', 'Lwin', 'thiri.lwin@demo.chic', '09970001007', 'No. 35, Bo Yar Nyunt Street, Dagon, Yangon'),
        (8, 'Su', 'Myat', 'su.myat@demo.chic', '09970001008', 'No. 16, Yaw Min Gyi Street, Dagon, Yangon'),
        (9, 'Khin', 'Htet', 'khin.htet@demo.chic', '09970001009', 'No. 54, Shwe Gon Daing Road, Bahan, Yangon'),
        (10, 'Wai', 'Yan', 'wai.yan@demo.chic', '09970001010', 'No. 91, Bayint Naung Road, Mayangone, Yangon'),
        (11, 'Zin', 'Mar', 'zin.mar@demo.chic', '09970001011', 'No. 23, 78th Street, Chan Aye Tharzan, Mandalay'),
        (12, 'Moe', 'Thet', 'moe.thet@demo.chic', '09970001012', 'No. 7, 35th Street, Mahar Aung Myay, Mandalay'),
        (13, 'Yu', 'Par', 'yu.par@demo.chic', '09970001013', 'No. 118, 26th Street, Aung Myay Tharzan, Mandalay'),
        (14, 'Cherry', 'Ko', 'cherry.ko@demo.chic', '09970001014', 'No. 44, Bogyoke Road, Taunggyi'),
        (15, 'Thet', 'Htar', 'thet.htar@demo.chic', '09970001015', 'No. 29, Circular Road, Pyin Oo Lwin'),
        (16, 'Yamin', 'Oo', 'yamin.oo@demo.chic', '09970001016', 'No. 67, Main Road, Mawlamyine'),
        (17, 'Sandi', 'Kyaw', 'sandi.kyaw@demo.chic', '09970001017', 'No. 102, Shwe Dagon Pagoda Road, Yangon'),
        (18, 'Phyu', 'Sin', 'phyu.sin@demo.chic', '09970001018', 'No. 31, Kan Street, Hlaing, Yangon'),
        (19, 'Lae', 'Lae', 'lae.lae@demo.chic', '09970001019', 'No. 59, Min Ye Kyaw Swar Road, Lanmadaw, Yangon'),
        (20, 'Khine', 'Zar', 'khine.zar@demo.chic', '09970001020', 'No. 84, Padauk Street, North Dagon, Yangon');

    INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
    SELECT
        seed.first_name,
        seed.last_name,
        seed.email,
        @CustomerPasswordHash,
        seed.address,
        seed.phone_number,
        DATEADD(day, -seed.seed_no, @StartDate),
        @CustomerRoleId
    FROM @SeedCustomers seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.users existing
        WHERE LOWER(existing.email) = LOWER(seed.email)
    );

    DECLARE @Customers table
    (
        seed_no int NOT NULL PRIMARY KEY,
        user_id int NOT NULL,
        email varchar(100) NOT NULL,
        address varchar(255) NOT NULL
    );

    INSERT INTO @Customers (seed_no, user_id, email, address)
    SELECT seed.seed_no, customer.user_id, customer.email, customer.address
    FROM @SeedCustomers seed
    INNER JOIN dbo.users customer ON LOWER(customer.email) = LOWER(seed.email)
    WHERE customer.role_id = @CustomerRoleId;

    IF (SELECT COUNT(*) FROM @Customers) <> 20
    BEGIN
        THROW 51001, 'Expected 20 demo customers with the customer role.', 1;
    END;

    DECLARE @Variants table
    (
        variant_row int IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        variant_id int NOT NULL,
        unit_price decimal(10, 2) NOT NULL
    );

    INSERT INTO @Variants (variant_id, unit_price)
    SELECT
        variant.variant_id,
        CAST(product.base_price + ISNULL(variant.price_modifier, 0) AS decimal(10, 2)) AS unit_price
    FROM dbo.product_variants variant
    INNER JOIN dbo.products product ON product.product_id = variant.product_id
    WHERE variant.stock_quantity > 0
    ORDER BY product.product_id, variant.variant_id;

    DECLARE @VariantCount int = (SELECT COUNT(*) FROM @Variants);

    IF @VariantCount = 0
    BEGIN
        THROW 51002, 'No product variants with stock are available for demo order generation.', 1;
    END;

    DECLARE @OrderNo int = 1;
    DECLARE @OrderId int;
    DECLARE @UserId int;
    DECLARE @CustomerNo int;
    DECLARE @ShippingAddress varchar(255);
    DECLARE @CreatedAt datetime2(0);
    DECLARE @OrderStatus varchar(20);
    DECLARE @OrderPaymentStatus varchar(20);
    DECLARE @PaymentMethod varchar(20);
    DECLARE @PaymentStatus varchar(20);
    DECLARE @TransactionId varchar(100);
    DECLARE @PaymentBucket int;
    DECLARE @SpanMinutes int = DATEDIFF(minute, @StartDate, @EndDate);
    DECLARE @ItemCount int;
    DECLARE @ItemNo int;
    DECLARE @VariantRow int;
    DECLARE @VariantId int;
    DECLARE @Quantity int;
    DECLARE @UnitPrice decimal(10, 2);
    DECLARE @OrderTotal decimal(10, 2);

    WHILE @OrderNo <= 200
    BEGIN
        SET @TransactionId = CONCAT('DEMO-ORD-', RIGHT(CONCAT('0000', @OrderNo), 4));

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.payments
            WHERE transaction_id = @TransactionId
        )
        BEGIN
            SET @CustomerNo = ((@OrderNo - 1) % 20) + 1;

            SELECT
                @UserId = customer.user_id,
                @ShippingAddress = customer.address
            FROM @Customers customer
            WHERE customer.seed_no = @CustomerNo;

            SET @CreatedAt = DATEADD(minute, ((@OrderNo - 1) * @SpanMinutes) / 199, @StartDate);

            IF @CreatedAt < '2026-05-01T00:00:00'
            BEGIN
                SET @OrderStatus = CASE WHEN @OrderNo % 10 IN (0, 1) THEN 'Processing' ELSE 'Confirm' END;
            END
            ELSE IF @CreatedAt < '2026-06-01T00:00:00'
            BEGIN
                SET @OrderStatus = CASE
                    WHEN @OrderNo % 6 IN (0, 1) THEN 'Pending'
                    WHEN @OrderNo % 6 IN (2, 3) THEN 'Processing'
                    ELSE 'Confirm'
                END;
            END
            ELSE
            BEGIN
                SET @OrderStatus = CASE
                    WHEN @OrderNo % 5 IN (0, 1, 2) THEN 'Pending'
                    WHEN @OrderNo % 5 = 3 THEN 'Processing'
                    ELSE 'Confirm'
                END;
            END;

            SET @PaymentBucket = @OrderNo % 20;
            SET @PaymentMethod = CASE
                WHEN @PaymentBucket BETWEEN 1 AND 8 THEN 'kpay'
                WHEN @PaymentBucket BETWEEN 9 AND 15 THEN 'wave_money'
                ELSE 'cod'
            END;

            IF @PaymentMethod = 'cod'
            BEGIN
                SET @PaymentStatus = CASE WHEN @OrderStatus = 'Confirm' THEN 'completed' ELSE 'pending' END;
                SET @OrderPaymentStatus = CASE WHEN @OrderStatus = 'Confirm' THEN 'paid' ELSE 'unpaid' END;
            END
            ELSE
            BEGIN
                SET @PaymentStatus = CASE WHEN @OrderStatus = 'Pending' AND @OrderNo % 3 = 0 THEN 'pending' ELSE 'completed' END;
                SET @OrderPaymentStatus = CASE WHEN @PaymentStatus = 'completed' THEN 'paid' ELSE 'unpaid' END;
            END;

            INSERT INTO dbo.orders (user_id, total_amount, order_status, payment_status, shipping_address, created_at)
            VALUES (@UserId, 0.00, @OrderStatus, @OrderPaymentStatus, @ShippingAddress, @CreatedAt);

            SET @OrderId = SCOPE_IDENTITY();
            SET @ItemCount = 1 + (@OrderNo % 4);
            SET @ItemNo = 1;

            WHILE @ItemNo <= @ItemCount
            BEGIN
                SET @VariantRow = (((@OrderNo * 7) + (@ItemNo * 13)) % @VariantCount) + 1;
                SET @Quantity = CASE
                    WHEN (@OrderNo + @ItemNo) % 7 = 0 THEN 3
                    WHEN (@OrderNo + @ItemNo) % 3 = 0 THEN 2
                    ELSE 1
                END;

                SELECT
                    @VariantId = variant.variant_id,
                    @UnitPrice = variant.unit_price
                FROM @Variants variant
                WHERE variant.variant_row = @VariantRow;

                INSERT INTO dbo.order_items (order_id, variant_id, quantity, price_at_purchase)
                VALUES (@OrderId, @VariantId, @Quantity, @UnitPrice);

                SET @ItemNo += 1;
            END;

            SELECT @OrderTotal = CAST(SUM(quantity * price_at_purchase) AS decimal(10, 2))
            FROM dbo.order_items
            WHERE order_id = @OrderId;

            UPDATE dbo.orders
            SET total_amount = @OrderTotal
            WHERE order_id = @OrderId;

            INSERT INTO dbo.payments
            (
                order_id,
                payment_method,
                transaction_id,
                slip_image_url,
                amount,
                payment_status,
                error_message,
                created_at
            )
            VALUES
            (
                @OrderId,
                @PaymentMethod,
                @TransactionId,
                NULL,
                @OrderTotal,
                @PaymentStatus,
                NULL,
                DATEADD(minute, 5, @CreatedAt)
            );
        END;

        SET @OrderNo += 1;
    END;

    COMMIT TRANSACTION;

    SELECT 'demo_customers' AS metric, COUNT(*) AS value
    FROM dbo.users
    WHERE email LIKE '%@demo.chic';

    SELECT 'demo_orders' AS metric, COUNT(*) AS value
    FROM dbo.payments
    WHERE transaction_id LIKE 'DEMO-ORD-%';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
