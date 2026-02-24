using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradeHistorySessions", Schema = "Trades")]
public sealed class TradeHistorySession : EntityBase<int>
{
    public int TradeHistoryId { get; set; }

    public int TradingSessionId { get; set; }

    [ForeignKey(nameof(TradeHistoryId))]

    public TradeHistory TradeHistory { get; set; }

    [ForeignKey(nameof(TradingSessionId))]
    public TradingSession TradingSession { get; set; }
}