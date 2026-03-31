namespace TradingJournal.Modules.Trades.Options;

public sealed class GoogleGenAIOptions
{
    public const string BindLocator = "GoogleGenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string MediaResolution { get; set; } = string.Empty;

    public string ResponseMimeType { get; set; } = string.Empty;

    public string ThinkingLevel { get; set; } = string.Empty;
}
