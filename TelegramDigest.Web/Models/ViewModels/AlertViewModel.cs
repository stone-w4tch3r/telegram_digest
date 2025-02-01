namespace TelegramDigest.Web.Models.ViewModels;

public class AlertViewModel
{
    public AlertType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Dismissible { get; set; } = true;

    public string CssClass =>
        Type switch
        {
            AlertType.Success => "alert-success",
            AlertType.Error => "alert-danger",
            AlertType.Warning => "alert-warning",
            AlertType.Info => "alert-info",
            _ => "alert-info",
        };
}

public enum AlertType
{
    Success,
    Error,
    Warning,
    Info,
}
