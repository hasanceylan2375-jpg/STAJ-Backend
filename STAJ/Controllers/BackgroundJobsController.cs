using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Jobs;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class BackgroundJobsController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IConfiguration _configuration;

        public BackgroundJobsController(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            IConfiguration configuration)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _configuration = configuration;
        }

        [HttpPost("trigger-log-maintenance")]
        public IActionResult TriggerLogMaintenance()
        {
            var jobId = _backgroundJobClient.Enqueue<ScheduledJobsService>(job => job.GunlukLogBakimiAsync());
            return Ok(new
            {
                jobId,
                message = "Log bakım işi kuyruğa alındı."
            });
        }

        [HttpPost("trigger-database-health-check")]
        public IActionResult TriggerDatabaseHealthCheck()
        {
            var jobId = _backgroundJobClient.Enqueue<ScheduledJobsService>(job => job.VeritabaniKontroluAsync());
            return Ok(new
            {
                jobId,
                message = "Veritabanı sağlık kontrolü kuyruğa alındı."
            });
        }

        [HttpPost("trigger-birthday-emails")]
        public IActionResult TriggerBirthdayEmails()
        {
            var jobId = _backgroundJobClient.Enqueue<ScheduledJobsService>(job => job.DogumGunuMailleriAsync());
            return Ok(new
            {
                jobId,
                message = "Doğum günü mail işi kuyruğa alındı."
            });
        }

        [HttpPost("register-recurring-jobs")]
        public IActionResult RegisterRecurringJobs()
        {
            var logCron = _configuration.GetValue<string>("BackgroundJobs:DailyLogMaintenanceCron") ?? "0 3 * * *";
            var healthCron = _configuration.GetValue<string>("BackgroundJobs:DatabaseHealthCheckCron") ?? "*/30 * * * *";
            var birthdayEmailCron = _configuration.GetValue<string>("BackgroundJobs:BirthdayEmailCron") ?? "0 9 * * *";

            _recurringJobManager.AddOrUpdate<ScheduledJobsService>(
                "daily-log-maintenance",
                job => job.GunlukLogBakimiAsync(),
                logCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            _recurringJobManager.AddOrUpdate<ScheduledJobsService>(
                "database-health-check",
                job => job.VeritabaniKontroluAsync(),
                healthCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            _recurringJobManager.AddOrUpdate<ScheduledJobsService>(
                "birthday-email",
                job => job.DogumGunuMailleriAsync(),
                birthdayEmailCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            return Ok(new
            {
                message = "Zamanlanmış işler kaydedildi.",
                dailyLogMaintenanceCron = logCron,
                databaseHealthCheckCron = healthCron,
                birthdayEmailCron
            });
        }

        [HttpDelete("recurring-jobs/{jobId}")]
        public IActionResult RemoveRecurringJob(string jobId)
        {
            _recurringJobManager.RemoveIfExists(jobId);
            return Ok(new { message = $"'{jobId}' zamanlanmış işi kaldırıldı." });
        }
    }
}
