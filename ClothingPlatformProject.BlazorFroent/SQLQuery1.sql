USE [ClothingPlatformDB];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

ALTER TABLE dbo.orders DROP CONSTRAINT CHK_OrderStatus;
GO

-- ၂။ 'Confirm' နှင့် 'Pending' ပါဝင်သော Constraint အသစ်ဆောက်ပါ
ALTER TABLE dbo.orders 
ADD CONSTRAINT CHK_OrderStatus 
CHECK (order_status IN ('Pending', 'Processing', 'Confirm', 'Shipped', 'Delivered', 'Cancelled'));
GO

-- ၃။ 'delivered' အားလုံးကို 'Confirm' သို့ ပြောင်းပြီး၊ 'pending' ကို 'Pending' သို့ ပြောင်းပါ
UPDATE dbo.orders
SET order_status = CASE LOWER(order_status)
    WHEN 'processing' THEN 'Processing'
    WHEN 'confirm' THEN 'Confirm'
    WHEN 'confirmed' THEN 'Confirm'
    WHEN 'completed' THEN 'Confirm'
    WHEN 'delivered' THEN 'Confirm'  -- သင်အလိုရှိသည့်အတိုင်း 'Confirm' သို့ ပြောင်းလဲပေးမည်
    WHEN 'pending' THEN 'Pending'    -- စာလုံးအသေး 'pending' ကို စာလုံးကြီး တင်ပေးမည်
    ELSE 'Pending'
END;
GO

UPDATE dbo.guest_orders
SET order_status = CASE LOWER(order_status)
    WHEN 'processing' THEN 'Processing'
    WHEN 'confirm' THEN 'Confirm'
    WHEN 'confirmed' THEN 'Confirm'
    WHEN 'completed' THEN 'Confirm'
    WHEN 'delivered' THEN 'Confirm'
    ELSE 'Pending'
END;

UPDATE dbo.payments
SET payment_method = CASE LOWER(payment_method)
    WHEN 'kbzpay' THEN 'kpay'
    WHEN 'kbz pay' THEN 'kpay'
    WHEN 'kpay' THEN 'kpay'
    WHEN 'wavemoney' THEN 'wave_money'
    WHEN 'wave pay' THEN 'wave_money'
    WHEN 'wavepay' THEN 'wave_money'
    WHEN 'wave_money' THEN 'wave_money'
    ELSE 'cod'
END;

IF COL_LENGTH('dbo.payments', 'slip_image_url') IS NULL
BEGIN
    ALTER TABLE dbo.payments ADD slip_image_url varchar(255) NULL;
END;

IF OBJECT_ID('dbo.customer_notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_notifications
    (
        notification_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_customer_notifications PRIMARY KEY,
        user_id int NOT NULL,
        title nvarchar(150) NOT NULL,
        message nvarchar(500) NOT NULL,
        order_id int NULL,
        is_read bit NOT NULL CONSTRAINT DF_customer_notifications_is_read DEFAULT (0),
        created_at datetime2(7) NOT NULL CONSTRAINT DF_customer_notifications_created_at DEFAULT (sysutcdatetime()),
        CONSTRAINT FK_CustomerNotifications_Users FOREIGN KEY (user_id) REFERENCES dbo.users(user_id) ON DELETE CASCADE
    );
END;

;WITH cart_totals AS
(
    SELECT user_id, variant_id, MIN(cart_id) AS keep_cart_id, SUM(quantity) AS total_quantity
    FROM dbo.cart_items
    GROUP BY user_id, variant_id
    HAVING COUNT(*) > 1
)
UPDATE ci
SET quantity = ct.total_quantity
FROM dbo.cart_items ci
JOIN cart_totals ct ON ct.keep_cart_id = ci.cart_id;

;WITH duplicates AS
(
    SELECT cart_id,
           ROW_NUMBER() OVER (PARTITION BY user_id, variant_id ORDER BY cart_id) AS row_num
    FROM dbo.cart_items
)
DELETE FROM duplicates
WHERE row_num > 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CartItems_User_Variant' AND object_id = OBJECT_ID('dbo.cart_items'))
BEGIN
    CREATE UNIQUE INDEX UX_CartItems_User_Variant ON dbo.cart_items(user_id, variant_id);
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_OrderStatus')
BEGIN
    ALTER TABLE dbo.orders DROP CONSTRAINT CHK_OrderStatus;
END;

ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CHK_OrderStatus
CHECK (order_status IN ('Pending', 'Processing', 'Confirm'));

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PaymentMethod')
BEGIN
    ALTER TABLE dbo.payments DROP CONSTRAINT CHK_PaymentMethod;
END;

ALTER TABLE dbo.payments WITH CHECK ADD CONSTRAINT CHK_PaymentMethod
CHECK (payment_method IN ('cod', 'kpay', 'wave_money'));

DECLARE @constraintName sysname;

SELECT @constraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.orders')
  AND c.name = 'order_status';

IF @constraintName IS NOT NULL
BEGIN
    -- ၁။ SQL command ကို သိမ်းဆည်းရန် variable တစ်ခု ကြေညာပါ
    DECLARE @sql NVARCHAR(MAX);
    
    -- ၂။ QUOTENAME ကို variable ထဲမှာ ကြိုတင် ပေါင်းစပ်ပေးပါ
    SET @sql = N'ALTER TABLE dbo.orders DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    
    -- ၃။ sp_executesql ကို သုံးပြီး run ပါ (EXEC ထက် ပိုမိုကောင်းမွန်ပြီး စိတ်ချရပါသည်)
    EXEC sp_executesql @sql;
END;

ALTER TABLE dbo.orders ADD CONSTRAINT DF_orders_order_status DEFAULT ('Pending') FOR order_status;

MERGE dbo.Tbl_Roles AS target
USING (VALUES
    ('admin', 'Full administrator access'),
    ('staff', 'Staff operations access')
) AS source(RoleName, Description)
ON target.RoleName = source.RoleName
WHEN NOT MATCHED THEN
    INSERT (RoleName, Description, CreatedAt) VALUES (source.RoleName, source.Description, GETDATE());

MERGE dbo.Tbl_Permissions AS target
USING (VALUES ('Reports.Generate', 'Generate and export admin reports')) AS source(PermissionName, Description)
ON target.PermissionName = source.PermissionName
WHEN NOT MATCHED THEN
    INSERT (PermissionName, Description, CreatedAt) VALUES (source.PermissionName, source.Description, GETDATE());

INSERT INTO dbo.Tbl_RolePermissions (RoleId, PermissionId, CreatedAt)
SELECT r.RoleId, p.PermissionId, GETDATE()
FROM dbo.Tbl_Roles r
CROSS JOIN dbo.Tbl_Permissions p
WHERE r.RoleName = 'admin'
  AND p.PermissionName = 'Reports.Generate'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.Tbl_RolePermissions rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );

INSERT INTO dbo.Tbl_Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, Address, RoleId, CreatedAt)
SELECT 'Admin', 'User', 'admin@boutique.com', 'admin123', '09252522525', 'Chic Boutique HQ', r.RoleId, GETDATE()
FROM dbo.Tbl_Roles r
WHERE r.RoleName = 'admin'
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_Users WHERE Email = 'admin@boutique.com');

INSERT INTO dbo.Tbl_Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, Address, RoleId, CreatedAt)
SELECT 'Thiri', 'San', 'staff@boutique.com', 'staff123', '09222333444', 'No. 456, Atelier Rd, Yangon', r.RoleId, GETDATE()
FROM dbo.Tbl_Roles r
WHERE r.RoleName = 'staff'
  AND NOT EXISTS (SELECT 1 FROM dbo.Tbl_Users WHERE Email = 'staff@boutique.com');

INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, role, created_at)
SELECT tu.FirstName, tu.LastName, tu.Email, tu.PasswordHash, ISNULL(tu.Address, ''), ISNULL(tu.PhoneNumber, ''), tr.RoleName, GETDATE()
FROM dbo.Tbl_Users tu
JOIN dbo.Tbl_Roles tr ON tr.RoleId = tu.RoleId
WHERE tr.RoleName IN ('admin', 'staff')
  AND NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.email = tu.Email);

COMMIT TRANSACTION;
GO
