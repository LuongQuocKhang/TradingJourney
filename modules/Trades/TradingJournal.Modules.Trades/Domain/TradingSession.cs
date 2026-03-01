using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradingSessions", Schema = "Trades")]
public sealed class TradingSession : EntityBase<int>
{
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public TradingSessionStatus Status { get; set; }
}
