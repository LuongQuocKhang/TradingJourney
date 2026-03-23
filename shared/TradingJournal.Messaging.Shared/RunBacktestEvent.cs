namespace TradingJournal.Messaging.Shared;

public sealed record RunBacktestEvent
{
    public int BacktestId { get; init; }
}
