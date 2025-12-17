using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NextCommerceShop.Models.Settings;
using System.Net;
using System.Net.Mail;

namespace NextCommerceShop.Services
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(
            IOptions<EmailSettings> options,
            ILogger<BrevoEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // DEBUG LOG - SEE EXACT VALUES USED AT RUNTIME
            _logger.LogInformation(
                "SMTP DEBUG -> Host={Host}, Port={Port}, Username={Username}, FromEmail={FromEmail}",
                _settings.Host,
                _settings.Port,
                _settings.Username,
                _settings.FromEmail
            );

            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mail.To.Add(email);

                await client.SendMailAsync(mail);

                _logger.LogInformation("Email successfully sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending FAILED");
                throw;
            }
        }
    }
}
