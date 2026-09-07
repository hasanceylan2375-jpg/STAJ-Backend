using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using STAJ.Data;
using STAJ.Hubs;

namespace STAJ.Jobs
{
    public class ScheduledJobsService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ScheduledJobsService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;

        public ScheduledJobsService(
            AppDbContext dbContext,
            ILogger<ScheduledJobsService> logger,
            IHubContext<NotificationHub> hubContext,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task GunlukLogBakimiAsync()
        {
            var retentionDays = _configuration.GetValue<int>("BackgroundJobs:AuditRetentionDays", 30);
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            try
            {
                var deletedCount = await _dbContext.AuditLogs
                    .Where(x => x.CreatedAt < cutoff)
                    .ExecuteDeleteAsync();

                _logger.LogInformation(
                    "Günlük log bakım işi tamamlandı. {DeletedCount} kayıt silindi. Saklama süresi: {RetentionDays} gün.",
                    deletedCount,
                    retentionDays);

                await _hubContext.Clients.All.SendAsync("backgroundJobTamamlandi", new
                {
                    job = "daily-log-maintenance",
                    message = "Günlük log bakım işi tamamlandı.",
                    deletedCount,
                    completedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük log bakım işi başarısız oldu.");

                await _hubContext.Clients.All.SendAsync("backgroundJobHatasi", new
                {
                    job = "daily-log-maintenance",
                    message = "Günlük log bakım işi başarısız oldu.",
                    error = ex.Message,
                    occurredAt = DateTime.UtcNow
                });

                throw;
            }
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task VeritabaniKontroluAsync()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (!canConnect)
                    throw new InvalidOperationException("PostgreSQL bağlantısı kurulamadı.");

                _logger.LogInformation("Veritabanı sağlık kontrolü başarılı.");

                await _hubContext.Clients.All.SendAsync("backgroundJobTamamlandi", new
                {
                    job = "database-health-check",
                    message = "Veritabanı sağlık kontrolü başarılı.",
                    completedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı sağlık kontrolü başarısız oldu.");

                await _hubContext.Clients.All.SendAsync("backgroundJobHatasi", new
                {
                    job = "database-health-check",
                    message = "Veritabanı sağlık kontrolü başarısız oldu.",
                    error = ex.Message,
                    occurredAt = DateTime.UtcNow
                });

                throw;
            }
        }
    }
}
