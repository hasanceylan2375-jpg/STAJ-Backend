using System.Net;
using System.Net.Mail;

namespace STAJ.Services
{
    public class MailService
    {
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMailAsync(string to, string subject, string body)
        {
            var host = _configuration["MailSettings:Host"];
            var port = _configuration.GetValue<int>("MailSettings:Port");
            var userName = _configuration["MailSettings:UserName"];
            var password = _configuration["MailSettings:Password"];
            var from = _configuration["MailSettings:From"];

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(userName, password)
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(from!),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
