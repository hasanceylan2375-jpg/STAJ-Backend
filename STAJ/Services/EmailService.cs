using System.Net;
using System.Net.Mail;

namespace STAJ.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            var port = _configuration.GetValue<int>("Smtp:Port", 587);
            var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username;
            var fromName = _configuration["Smtp:FromName"] ?? "STAJ";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("SMTP ayarları eksik. Smtp:Host, Username, Password ve FromEmail ayarlarını doldurun.");
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);
            await client.SendMailAsync(message);
        }
    }
}
