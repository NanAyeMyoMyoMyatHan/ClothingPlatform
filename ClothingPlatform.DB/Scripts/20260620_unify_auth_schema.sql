USE [ClothingPlatformDB];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.roles
    (
        role_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_roles PRIMARY KEY,
        role_name nvarchar(50) NOT NULL,
        description nvarchar(255) NULL,
        created_at datetime NULL CONSTRAINT DF_roles_created_at DEFAULT (GETDATE()),
        CONSTRAINT UQ_roles_role_name UNIQUE (role_name)
    );
END;

IF OBJECT_ID('dbo.permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.permissions
    (
        permission_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_permissions PRIMARY KEY,
        permission_name nvarchar(100) NOT NULL,
        description nvarchar(255) NULL,
        created_at datetime NULL CONSTRAINT DF_permissions_created_at DEFAULT (GETDATE()),
        CONSTRAINT UQ_permissions_permission_name UNIQUE (permission_name)
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
    (N'admin', N'Full administrator access'),
    (N'staff', N'Staff operations access'),
    (N'customer', N'Customer shopping account')
) AS source(role_name, description)
ON target.role_name = source.role_name
WHEN MATCHED THEN
    UPDATE SET description = source.description
WHEN NOT MATCHED THEN
    INSERT (role_name, description, created_at)
    VALUES (source.role_name, source.description, GETDATE());

MERGE dbo.permissions AS target
USING (VALUES
    (N'Reports.Generate', N'Generate and export admin reports')
) AS source(permission_name, description)
ON target.permission_name = source.permission_name
WHEN MATCHED THEN
    UPDATE SET description = source.description
WHEN NOT MATCHED THEN
    INSERT (permission_name, description, created_at)
    VALUES (source.permission_name, source.description, GETDATE());

INSERT INTO dbo.role_permissions (role_id, permission_id, created_at)
SELECT r.role_id, p.permission_id, GETDATE()
FROM dbo.roles r
CROSS JOIN dbo.permissions p
WHERE r.role_name = N'admin'
  AND p.permission_name = N'Reports.Generate'
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

IF OBJECT_ID('dbo.Tbl_Users', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Tbl_Roles', 'U') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        SELECT tu.FirstName,
               tu.LastName,
               LOWER(tu.Email),
               tu.PasswordHash,
               ISNULL(tu.Address, ''''),
               ISNULL(tu.PhoneNumber, ''''),
               ISNULL(tu.CreatedAt, GETDATE()),
               r.role_id
        FROM dbo.Tbl_Users tu
        JOIN dbo.Tbl_Roles tr ON tr.RoleId = tu.RoleId
        JOIN dbo.roles r ON r.role_name = tr.RoleName
        WHERE tr.RoleName IN (N''admin'', N''staff'', N''customer'')
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.users u
              WHERE LOWER(u.email) = LOWER(tu.Email)
          );

        UPDATE u
        SET role_id = r.role_id
        FROM dbo.users u
        JOIN dbo.Tbl_Users tu ON LOWER(tu.Email) = LOWER(u.email)
        JOIN dbo.Tbl_Roles tr ON tr.RoleId = tu.RoleId
        JOIN dbo.roles r ON r.role_name = tr.RoleName
        WHERE u.role_id IS NULL;';
END;

DECLARE @adminRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = N'admin');
DECLARE @staffRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = N'staff');
DECLARE @customerRoleId int = (SELECT role_id FROM dbo.roles WHERE role_name = N'customer');

EXEC sp_executesql N'
    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''admin@boutique.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Admin'', ''User'', ''admin@boutique.com'', ''admin123'', ''Chic Boutique HQ'', ''09252522525'', GETDATE(), @adminRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users
        SET role_id = @adminRoleId
        WHERE email = ''admin@boutique.com'';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''staff@boutique.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Thiri'', ''San'', ''staff@boutique.com'', ''staff123'', ''No. 456, Atelier Rd, Yangon'', ''09222333444'', GETDATE(), @staffRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users
        SET role_id = @staffRoleId
        WHERE email = ''staff@boutique.com'';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = ''emily@gmail.com'')
    BEGIN
        INSERT INTO dbo.users (first_name, last_name, email, password_hash, address, phone_number, created_at, role_id)
        VALUES (''Emily'', ''Watson'', ''emily@gmail.com'', ''12345678'', ''No. 789, Style Street, Yangon'', ''09999888777'', GETDATE(), @customerRoleId);
    END
    ELSE
    BEGIN
        UPDATE dbo.users
        SET role_id = @customerRoleId
        WHERE email = ''emily@gmail.com'';
    END;

    UPDATE dbo.users
    SET role_id = @customerRoleId
    WHERE role_id IS NULL;',
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
    DECLARE @roleDefaultName sysname;
    SELECT @roleDefaultName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.users')
      AND c.name = 'role';

    IF @roleDefaultName IS NOT NULL
    BEGIN
        DECLARE @dropDefaultSql nvarchar(max) =
            N'ALTER TABLE dbo.users DROP CONSTRAINT ' + QUOTENAME(@roleDefaultName) + N';';
        EXEC sys.sp_executesql @dropDefaultSql;
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

IF OBJECT_ID('dbo.Tbl_RolePermissions', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Tbl_RolePermissions;
END;

IF OBJECT_ID('dbo.Tbl_Users', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Tbl_Users;
END;

IF OBJECT_ID('dbo.Tbl_Permissions', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Tbl_Permissions;
END;

IF OBJECT_ID('dbo.Tbl_Roles', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Tbl_Roles;
END;

COMMIT TRANSACTION;
GO
