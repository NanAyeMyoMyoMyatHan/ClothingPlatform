using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClothingPlatform.DB.AppDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClothingPlatform.Api.Features.Notifications
{
    public class NotificationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationCleanupService> _logger;

        public NotificationCleanupService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running notification cleanup job...");
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var threshold = DateTime.Now.AddMonths(-6);
                        
                        var oldNotifications = await db.CustomerNotifications
                            .Where(n => n.CreatedAt < threshold)
                            .ToListAsync(stoppingToken);

                        if (oldNotifications.Any())
                        {
                            db.CustomerNotifications.RemoveRange(oldNotifications);
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Deleted {Count} notifications older than 6 months.", oldNotifications.Count);
                        }
                        else
                        {
                            _logger.LogInformation("No old notifications found to delete.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification cleanup job.");
                }

                // Run once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Notification Cleanup Service is stopping.");
        }
    }
}
