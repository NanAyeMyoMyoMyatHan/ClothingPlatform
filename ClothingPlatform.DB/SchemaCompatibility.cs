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
                    created_at DATETIME NOT NULL DEFAULT GETDATE()
                );
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
            """;

        return db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
