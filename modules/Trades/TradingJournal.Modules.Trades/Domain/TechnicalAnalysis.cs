using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TechnicalAnalysis", Schema = "Trades")]
public sealed class TechnicalAnalysis : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
}
