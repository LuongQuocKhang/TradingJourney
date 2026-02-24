using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradingSessions", Schema = "Trades")]
public sealed class TradingSession : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;

    public string FromTime { get; set; } = string.Empty;

    public string ToTime { get; set; } = string.Empty;
}
