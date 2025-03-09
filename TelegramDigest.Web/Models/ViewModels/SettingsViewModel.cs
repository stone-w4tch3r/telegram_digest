using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TelegramDigest.Backend.Core;
using Host = TelegramDigest.HostHandler.Host;

namespace TelegramDigest.Web.Models.ViewModels;

public sealed record class SettingsViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Recipient Email")]
    public required string RecipientEmail { get; init; }

    [Required]
    [Display(Name = "SMTP Server")]
    [ModelBinder(typeof(HostModelBinder))]
    public required Host SmtpHost { get; init; }

    [Required]
    [Display(Name = "SMTP Port")]
    [Range(1, 65535)]
    public required int SmtpPort { get; init; }

    [Required]
    [Display(Name = "SMTP Username")]
    public required string SmtpUsername { get; init; }

    [Required]
    [Display(Name = "SMTP Password")]
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
    [Display(Name = "OpenAI Endpoint")]
    public required Uri OpenAiEndpoint { get; init; }

    [Required]
    [Display(Name = "Digest Time (UTC)")]
    [DataType(DataType.Time)]
    public required TimeOnly DigestTimeUtc { get; init; }

    [Required]
    [Display(Name = "Post Summary System Prompt")]
    public required string PromptPostSummarySystem { get; init; }

    [Required]
    [Display(Name = "Post Summary User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptPostSummaryUser { get; init; }

    [Required]
    [Display(Name = "Post Importance System Prompt")]
    public required string PromptPostImportanceSystem { get; init; }

    [Required]
    [Display(Name = "Post Importance User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptPostImportanceUser { get; init; }

    [Required]
    [Display(Name = "Digest Summary System Prompt")]
    public required string PromptDigestSummarySystem { get; init; }

    [Required]
    [Display(Name = "Digest Summary User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptDigestSummaryUser { get; init; }
}
