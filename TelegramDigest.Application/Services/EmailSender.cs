using FluentResults;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace TelegramDigest.Application.Services;

public class EmailSender
{
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(SettingsManager settingsManager, ILogger<EmailSender> logger)
    {
        _settingsManager = settingsManager;
        _logger = logger;
    }

    /// <summary>
    /// Sends digest email with a link to the web UI, avoiding complex HTML formatting
    /// </summary>
    public async Task<Result> SendDigest(DigestSummaryModel digest, string emailTo)
    {
        var settingsResult = await _settingsManager.LoadSettings();
        if (settingsResult.IsFailed)
            return settingsResult.ToResult();

        var settings = settingsResult.Value.SmtpSettings;

        try
        {
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                Credentials = new System.Net.NetworkCredential(settings.Username, settings.Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(settings.Username),
                Subject = $"Telegram Digest - {digest.CreatedAt:d MMMM yyyy}",
                Body = CreateEmailBody(digest),
                IsBodyHtml = false
            };
            message.To.Add(emailTo);

            await client.SendMailAsync(message);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send digest email to {EmailTo}", emailTo);
            return Result.Fail(new Error("Email sending failed").CausedBy(ex));
        }
    }

    private string CreateEmailBody(DigestSummaryModel digest) =>
$@"Your Telegram Digest for {digest.CreatedAt:d MMMM yyyy}

{digest.Title}

Posts: {digest.PostsCount}
Average Importance: {digest.AverageImportance}/3

View full digest: https://your-app-url/digest/{digest.DigestId}

To unsubscribe or change settings, visit: https://your-app-url/settings";
}
