using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RuntimeNullables;
using TelegramDigest.Backend.Models;
using Host = TelegramDigest.Types.Host.Host;

namespace TelegramDigest.Web.Models.ViewModels;

[NullChecks(false)]
public sealed record SettingsViewModel
{
    [EmailAddress]
    [Display(Name = "Recipient Email")]
    public required string RecipientEmail { get; init; }

    [Display(Name = "SMTP Server")]
    [ModelBinder(typeof(HostModelBinder))]
    public required Host SmtpHost { get; init; }

    [Display(Name = "SMTP Port")]
    [Range(1, 65535)]
    public required int SmtpPort { get; init; }

    [Display(Name = "SMTP Username")]
    public required string SmtpUsername { get; init; }

    [Display(Name = "SMTP Password")]
    public required string SmtpPassword { get; init; }

    [Display(Name = "SMTP Use SSL")]
    public required bool SmtpUseSsl { get; init; }

    [Display(Name = "OpenAI API Key")]
    public required string OpenAiApiKey { get; init; }

    [Display(Name = "OpenAI Model name")]
    public required string OpenAiModel { get; init; }

    [Display(Name = "OpenAI Max Tokens")]
    public required int OpenAiMaxToken { get; init; }

    [Display(Name = "OpenAI Endpoint")]
    public required Uri OpenAiEndpoint { get; init; }

    [Display(Name = "Digest Time (UTC)")]
    [DataType(DataType.Time)]
    public required TimeOnly DigestTimeUtc { get; init; }

    [Display(Name = "Post Summary System Prompt")]
    public required string PromptPostSummarySystem { get; init; }

    [Display(Name = "Post Summary User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptPostSummaryUser { get; init; }

    [Display(Name = "Post Importance System Prompt")]
    public required string PromptPostImportanceSystem { get; init; }

    [Display(Name = "Post Importance User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptPostImportanceUser { get; init; }

    [Display(Name = "Digest Summary System Prompt")]
    public required string PromptDigestSummarySystem { get; init; }

    [Display(Name = "Digest Summary User Prompt")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public required TemplateWithContent PromptDigestSummaryUser { get; init; }
}
