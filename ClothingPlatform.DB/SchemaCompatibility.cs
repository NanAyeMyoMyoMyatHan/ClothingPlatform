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

                IF @definition IS NULL OR @definition NOT LIKE '%Cancelled%'
                BEGIN
                    ALTER TABLE dbo.orders DROP CONSTRAINT CHK_OrderStatus;
                    ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CHK_OrderStatus
                    CHECK (order_status IN ('Pending', 'Processing', 'Confirm', 'Cancelled'));
                END
            END
            ELSE
            BEGIN
                ALTER TABLE dbo.orders WITH CHECK ADD CONSTRAINT CHK_OrderStatus
                CHECK (order_status IN ('Pending', 'Processing', 'Confirm', 'Cancelled'));
            END;
            """;

        return db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
