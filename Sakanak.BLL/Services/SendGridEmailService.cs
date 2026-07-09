using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sakanak.BLL.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace Sakanak.BLL.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("SendGrid API key is not configured.");
                throw new Exception("SendGrid API key is not configured.");
            }

            var client = new SendGridClient(apiKey);
            var senderEmail = _configuration["SendGrid:FromEmail"] ?? _configuration["SendGrid:SenderEmail"];
            var senderName = _configuration["SendGrid:FromName"] ?? _configuration["SendGrid:SenderName"];
            var replyToEmail = _configuration["SendGrid:ReplyToEmail"];

            var from = new EmailAddress(senderEmail, senderName);
            var toEmail = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, plainTextContent: "Please view this email in an HTML compatible client.", htmlContent: htmlContent);
            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                msg.ReplyTo = new EmailAddress(replyToEmail, senderName);
            }

            var response = await client.SendEmailAsync(msg);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {To}. Status Code: {StatusCode}. Response: {Response}", to, response.StatusCode, responseBody);
                return;
            }

            _logger.LogInformation("Email successfully sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to {To}", to);
        }
    }
}
