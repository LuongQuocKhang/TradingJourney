namespace TradingJournal.Shared.Abstractions;

public sealed record Error(string Code, string Description, string? stackTrace = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error SqlException => new("Error.SqlException", $"Database error occurred.");

    public static Error ValidationError(string errorCode, string errorMessage) => new(errorCode, errorMessage);

    public static Error UnexpectedError => new("Error.UnexpectedError", $"An unexpected error occurred.");

    public static Error InvalidInput => new("Error.InvalidInput", $"Invalid input.");

    public static Error LoggedPhoneNumberNotFound => new("Error.LoggedPhoneNumberNotFound", $"Không tìm thấy số điện thoại đăng nhập trong JWT.");
}
