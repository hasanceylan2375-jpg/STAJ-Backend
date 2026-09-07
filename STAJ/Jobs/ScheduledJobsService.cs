using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using STAJ.Data;
using STAJ.Hubs;
using STAJ.Services;

namespace STAJ.Jobs
{
    public class ScheduledJobsService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ScheduledJobsService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ScheduledJobsService(
            AppDbContext dbContext,
            ILogger<ScheduledJobsService> logger,
            IHubContext<NotificationHub> hubContext,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _hubContext = hubContext;
            _configuration = configuration;
            _emailService = emailService;
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

        [AutomaticRetry(Attempts = 2)]
        public async Task DogumGunuMailleriAsync()
        {
            var today = DateTime.Today;

            try
            {
                var customers = await _dbContext.Musteriler
                    .AsNoTracking()
                    .Where(x => x.DogumTarihi.HasValue
                        && x.DogumTarihi.Value.Month == today.Month
                        && x.DogumTarihi.Value.Day == today.Day
                        && x.Email != null
                        && x.Email != "")
                    .ToListAsync();

                var sentCount = 0;

                foreach (var customer in customers)
                {
                    var fullName = $"{customer.Ad} {customer.Soyad}".Trim();
                    var subject = "Doğum Gününüz Kutlu Olsun! 🎂";
                    var body = $"<html><body><h2>Doğum Gününüz Kutlu Olsun, {System.Net.WebUtility.HtmlEncode(fullName)}! 🎉</h2><p>Size sağlık, mutluluk ve güzel bir yaş dileriz.</p><p>İyi ki doğdunuz!</p><p><strong>STAJ</strong></p></body></html>";

                    await _emailService.SendAsync(customer.Email!, subject, body);
                    sentCount++;

                    _logger.LogInformation(
                        "Doğum günü maili gönderildi. Müşteri: {CustomerId}, Email: {Email}",
                        customer.Id,
                        customer.Email);
                }

                _logger.LogInformation(
                    "Doğum günü mail işi tamamlandı. {CustomerCount} doğum günü bulundu, {SentCount} mail gönderildi.",
                    customers.Count,
                    sentCount);

                await _hubContext.Clients.All.SendAsync("backgroundJobTamamlandi", new
                {
                    job = "birthday-email",
                    message = $"Doğum günü mail işi tamamlandı. {sentCount} mail gönderildi.",
                    sentCount,
                    completedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğum günü mail işi başarısız oldu.");

                await _hubContext.Clients.All.SendAsync("backgroundJobHatasi", new
                {
                    job = "birthday-email",
                    message = "Doğum günü mail işi başarısız oldu.",
                    error = ex.Message,
                    occurredAt = DateTime.UtcNow
                });

                throw;
            }
        }
    }
}
