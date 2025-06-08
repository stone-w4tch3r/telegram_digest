using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace TelegramDigest.Web.Models.ViewModels;

[NullChecks(false)]
public sealed class DigestGenerationViewModel : IValidatableObject
{
    [Display(Name = "From Date")]
    public required DateTime DateFrom { get; init; }

    [Display(Name = "To Date")]
    public required DateTime DateTo { get; init; }

    [Display(Name = "Selected Feed Urls")]
    public string[] SelectedFeedUrls { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateTo <= DateFrom)
        {
            yield return new(
                "End date must be after start date",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }

        if (
            DateFrom.ToUniversalTime() > DateTime.UtcNow
            || DateTo.ToUniversalTime() > DateTime.UtcNow
        )
        {
            yield return new(
                "Cannot generate digest for future dates",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }

        if (SelectedFeedUrls.Length == 0)
        {
            yield return new("At least one feed must be selected", [nameof(SelectedFeedUrls)]);
        }
    }
}
