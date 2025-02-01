using System.Net.Mail;
using FluentResults;

namespace TelegramDigest.Application.Core;

internal interface IEmailSender
{
    /// <summary>
    /// Sends digest email with a link to the web UI, avoiding complex HTML formatting
    /// </summary>
    public Task<Result> SendDigest(DigestSummaryModel digest);
}

internal sealed class EmailSender(ISettingsManager settingsManager, ILogger<EmailSender> logger)
    : IEmailSender
{
    /// <summary>
    /// Sends digest email with a link to the web UI, avoiding complex HTML formatting
    /// </summary>
    public async Task<Result> SendDigest(DigestSummaryModel digest)
    {
        var settingsResult = await settingsManager.LoadSettings();
        if (settingsResult.IsFailed)
        {
            return Result.Fail(settingsResult.Errors);
        }

        var emailTo = settingsResult.Value.EmailRecipient;
        var smtpSettings = settingsResult.Value.SmtpSettings;

        try
        {
            using var client = new SmtpClient(smtpSettings.Host, smtpSettings.Port);
            client.EnableSsl = smtpSettings.UseSsl;
            client.Credentials = new System.Net.NetworkCredential(
                smtpSettings.Username,
                smtpSettings.Password
            );

            var message = new MailMessage
            {
                From = new(smtpSettings.Username),
                Subject = $"Telegram Digest - {digest.CreatedAt:d MMMM yyyy}",
                Body = CreateEmailBody(digest),
                IsBodyHtml = false,
            };
            message.To.Add(emailTo);

            await client.SendMailAsync(message);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send digest email to {EmailTo}", emailTo);
            return Result.Fail(new Error("Email sending failed").CausedBy(ex));
        }
    }

    private static string CreateEmailBody(DigestSummaryModel digest) =>
        $"""
            Your Telegram Digest for {digest.CreatedAt:d MMMM yyyy}

            {digest.Title}

            Posts: {digest.PostsCount}
            Average Importance: {digest.AverageImportance}/3

            View full digest: https://your-app-url/digest/{digest.DigestId}

            To unsubscribe or change settings, visit: https://your-app-url/settings
            """;
}
