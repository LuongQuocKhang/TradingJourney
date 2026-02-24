using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "RiskGuardrails", Schema = "Trades")]
public sealed class RiskGuardrail : EntityBase<int>
{
    public double? AccountEquity { get; set; }

    public double? RiskPercentage { get; set; }

    public double? MaxDailyLoss { get; set; }

    public double? TakeProfit { get; set; }

    public double? PositionSize { get; set; }
}
