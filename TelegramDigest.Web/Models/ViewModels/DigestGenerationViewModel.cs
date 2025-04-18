using System.ComponentModel.DataAnnotations;

namespace TelegramDigest.Web.Models.ViewModels;

public sealed class DigestGenerationViewModel : IValidatableObject
{
    [Required]
    [Display(Name = "From Date")]
    public required DateTime DateFrom { get; init; }

    [Required]
    [Display(Name = "To Date")]
    public required DateTime DateTo { get; init; }

    [Display(Name = "Selected Channels")]
    public required string[] SelectedChannels { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateTo <= DateFrom)
        {
            yield return new ValidationResult(
                "End date must be after start date",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }

        if (
            DateFrom.ToUniversalTime() > DateTime.UtcNow
            || DateTo.ToUniversalTime() > DateTime.UtcNow
        )
        {
            yield return new ValidationResult(
                "Cannot generate digest for future dates",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }
    }
}
