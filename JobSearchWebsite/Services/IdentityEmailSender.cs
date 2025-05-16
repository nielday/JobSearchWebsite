using Microsoft.AspNetCore.Identity.UI.Services;
using JobSearchWebsite.Services;
using Microsoft.Extensions.Logging; // Thêm namespace này

namespace JobSearchWebsite.Services
{
    public class IdentityEmailSender : IEmailSender
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<IdentityEmailSender> _logger; // Thêm logger

        public IdentityEmailSender(IEmailService emailService, ILogger<IdentityEmailSender> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation($"Bắt đầu gửi email tới {email} với tiêu đề: {subject}");
            try
            {
                await _emailService.SendEmailAsync(email, subject, htmlMessage);
                _logger.LogInformation($"Gửi email thành công tới {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Gửi email tới {email} thất bại");
                throw; // Ném lại exception để không che giấu lỗi
            }
        }
    }
}