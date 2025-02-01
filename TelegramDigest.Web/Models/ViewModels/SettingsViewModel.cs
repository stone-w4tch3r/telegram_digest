using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public class SettingsViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Recipient Email")]
    public required string RecipientEmail { get; init; }

    [Required]
    [Display(Name = "SMTP Server")]
    [DataType(DataType.Url)] //TODO fix, prevents using hostname
    public required string SmtpHost { get; init; }

    [Required]
    [Display(Name = "SMTP Port")]
    [Range(1, 65535)]
    public required int SmtpPort { get; init; }

    [Required]
    [Display(Name = "SMTP Username")]
    public required string SmtpUsername { get; init; }

    [Required]
    [Display(Name = "SMTP Password")]
    [DataType(DataType.Password)] //TODO fix, hides password
    public required string SmtpPassword { get; init; }

    [Required]
    [Display(Name = "SMTP Use SSL")]
    public required bool SmtpUseSsl { get; init; }

    [Required]
    [Display(Name = "OpenAI API Key")]
    public required string OpenAiApiKey { get; init; }

    [Required]
    [Display(Name = "OpenAI Model name")]
    public required string OpenAiModel { get; init; }

    [Required]
    [Display(Name = "OpenAI Max Tokens")]
    public required int OpenAiMaxToken { get; init; }

    [Required]
    [Display(Name = "Digest Time (UTC)")]
    [DataType(DataType.Time)]
    public required TimeOnly DigestTimeUtc { get; init; }
}
