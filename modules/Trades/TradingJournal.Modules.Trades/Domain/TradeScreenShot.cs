using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "Screenshots", Schema = "Trades")]
public sealed class TradeScreenShot : EntityBase<int>
{
    public string Url { get; set; } = string.Empty;
}
