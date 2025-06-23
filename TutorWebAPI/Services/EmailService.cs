using System.Net;
using System.Net.Mail;

namespace TutorWebAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendVerificationEmail(string email, string code)
        {
            using (var smtpClient = new SmtpClient(_config["Smtp:Host"]))
            {
                smtpClient.Port = int.Parse(_config["Smtp:Port"]);
                smtpClient.Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Password"]);
                smtpClient.EnableSsl = true;

                var message = new MailMessage
                {
                    From = new MailAddress(_config["Smtp:User"]),
                    Subject = "Xác minh tài khoản",
                    Body = $"Mã xác minh tài khoản của bạn là : {code}",
                    IsBodyHtml = true
                };
                message.To.Add(email);

                try
                {
                    await smtpClient.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Lỗi khi gửi email: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
    }
}
