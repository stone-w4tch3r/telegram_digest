using FluentResults;

namespace TelegramDigest.Application.Shared;

public class NotFoundError : Error
{
    public NotFoundError(string message)
        : base(message) { }
}
