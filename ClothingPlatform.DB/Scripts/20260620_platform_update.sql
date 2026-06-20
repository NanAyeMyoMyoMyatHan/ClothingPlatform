USE [ClothingPlatformDB];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

UPDATE dbo.orders
SET order_status = CASE LOWER(order_status)
    WHEN 'processing' THEN 'Processing'
    WHEN 'confirm' THEN 'Confirm'
    WHEN 'confirmed' THEN 'Confirm'
    WHEN 'completed' THEN 'Confirm'
    WHEN 'delivered' THEN 'Confirm'
    ELSE 'Pending'
END;

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
    DECLARE @dropDefaultSql nvarchar(max);
    SET @dropDefaultSql = N'ALTER TABLE dbo.orders DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sys.sp_executesql @dropDefaultSql;
END;

ALTER TABLE dbo.orders ADD CONSTRAINT DF_orders_order_status DEFAULT ('Pending') FOR order_status;

IF OBJECT_ID('dbo.roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.roles
    (
        role_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_roles PRIMARY KEY,
        role_name nvarchar(50) NOT NULL CONSTRAINT UQ_roles_role_name UNIQUE,
        description nvarchar(255) NULL,
        created_at datetime NULL CONSTRAINT DF_roles_created_at DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID('dbo.permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.permissions
    (
        permission_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_permissions PRIMARY KEY,
        permission_name nvarchar(100) NOT NULL CONSTRAINT UQ_permissions_permission_name UNIQUE,
        description nvarchar(255) NULL,
        created_at datetime NULL CONSTRAINT DF_permissions_created_at DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID('dbo.role_permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.role_permissions
    (
        role_id int NOT NULL,
        permission_id int NOT NULL,
        created_at datetime NULL CONSTRAINT DF_role_permissions_created_at DEFAULT (GETDATE()),
        CONSTRAINT PK_role_permissions PRIMARY KEY (role_id, permission_id),
        CONSTRAINT FK_role_permissions_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(role_id),
        CONSTRAINT FK_role_permissions_permissions FOREIGN KEY (permission_id) REFERENCES dbo.permissions(permission_id)
    );
END;

MERGE dbo.roles AS target
USING (VALUES
    ('admin', 'Full administrator access'),
    ('staff', 'Staff operations access'),
    ('customer', 'Customer shopping account')
) AS source(role_name, description)
ON target.role_name = source.role_name
WHEN MATCHED THEN
    UPDATE SET description = source.description
WHEN NOT MATCHED THEN
    INSERT (role_name, description, created_at) VALUES (source.role_name, source.description, GETDATE());

MERGE dbo.permissions AS target
USING (VALUES ('Reports.Generate', 'Generate and export admin reports')) AS source(permission_name, description)
ON target.permission_name = source.permission_name
WHEN MATCHED THEN
    UPDATE SET description = source.description
WHEN NOT MATCHED THEN
    INSERT (permission_name, description, created_at) VALUES (source.permission_name, source.description, GETDATE());

INSERT INTO dbo.role_permissions (role_id, permission_id, created_at)
SELECT r.role_id, p.permission_id, GETDATE()
FROM dbo.roles r
CROSS JOIN dbo.permissions p
WHERE r.role_name = 'admin'
  AND p.permission_name = 'Reports.Generate'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.role_permissions rp
      WHERE rp.role_id = r.role_id
        AND rp.permission_id = p.permission_id
  );

IF COL_LENGTH('dbo.users', 'role_id') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD role_id int NULL;
END;

IF COL_LENGTH('dbo.users', 'role') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        UPDATE u
        SET role_id = r.role_id
        FROM dbo.users u
        JOIN dbo.roles r ON r.role_name = u.role
        WHERE u.role_id IS NULL;';
END;

DECLARE @adminRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = 'admin');
DECLARE @staffRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = 'staff');
DECLARE @customerRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = 'customer');

EXEC sp_executesql N'
    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''admin@boutique.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Admin'', ''User'', ''admin@boutique.com'', ''admin123'', ''Chic Boutique HQ'', ''09252522525'', GETDATE(), @adminRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users SET role_id = @adminRoleId WHERE email = ''admin@boutique.com'';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''staff@boutique.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Thiri'', ''San'', ''staff@boutique.com'', ''staff123'', ''No. 456, Atelier Rd, Yangon'', ''09222333444'', GETDATE(), @staffRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users SET role_id = @staffRoleId WHERE email = ''staff@boutique.com'';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''emily@gmail.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Emily'', ''Watson'', ''emily@gmail.com'', ''12345678'', ''No. 789, Style Street, Yangon'', ''09999888777'', GETDATE(), @customerRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users SET role_id = @customerRoleId WHERE email = ''emily@gmail.com'';
    END;

    UPDATE dbo.users SET role_id = @customerRoleId WHERE role_id IS NULL;',
    N'@adminRoleId int, @staffRoleId int, @customerRoleId int',
    @adminRoleId = @adminRoleId,
    @staffRoleId = @staffRoleId,
    @customerRoleId = @customerRoleId;

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_users_role_id_roles'
      AND parent_object_id = OBJECT_ID('dbo.users')
)
BEGIN
    ALTER TABLE dbo.users DROP CONSTRAINT FK_users_role_id_roles;
END;

EXEC sp_executesql N'ALTER TABLE dbo.users ALTER COLUMN role_id int NOT NULL;';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_users_role_id_roles'
      AND parent_object_id = OBJECT_ID('dbo.users')
)
BEGIN
    EXEC sp_executesql N'
        ALTER TABLE dbo.users WITH CHECK
        ADD CONSTRAINT FK_users_role_id_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(role_id);';
END;

IF COL_LENGTH('dbo.users', 'role') IS NOT NULL
BEGIN
    DECLARE @userRoleDefaultName sysname;
    SELECT @userRoleDefaultName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.users')
      AND c.name = 'role';

    IF @userRoleDefaultName IS NOT NULL
    BEGIN
        DECLARE @dropUserRoleDefaultSql nvarchar(max);
        SET @dropUserRoleDefaultSql = N'ALTER TABLE dbo.users DROP CONSTRAINT ' + QUOTENAME(@userRoleDefaultName);
        EXEC sys.sp_executesql @dropUserRoleDefaultSql;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.users')
          AND name = 'CHK_UserRole'
    )
    BEGIN
        ALTER TABLE dbo.users DROP CONSTRAINT CHK_UserRole;
    END;

    ALTER TABLE dbo.users DROP COLUMN role;
END;

COMMIT TRANSACTION;
GO
