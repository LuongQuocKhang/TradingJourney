using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Modules.Strategies.Common.Enums;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Strategies.Domain;

[Table(name: "StrategyTemplates", Schema = "Strategies")]
public sealed class StrategyTemplate : EntityBase<int>
{
    #region Identity

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StrategyCategory Category { get; set; }

    #endregion

    #region Configuration

    public string Asset { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public DateTime? DateRangeStart { get; set; }

    public DateTime? DateRangeEnd { get; set; }

    /// <summary>
    /// JSON serialized array of entry indicator strings
    /// </summary>
    public string EntryIndicators { get; set; } = "[]";

    /// <summary>
    /// JSON serialized array of exit indicator strings
    /// </summary>
    public string ExitIndicators { get; set; } = "[]";

    public double RiskPerTrade { get; set; }

    public StopLossType StopLossType { get; set; }

    public double StopLossValue { get; set; }

    public TakeProfitType TakeProfitType { get; set; }

    public double TakeProfitValue { get; set; }

    public PositionSizingType PositionSizing { get; set; }

    public double PositionSizeValue { get; set; }

    #endregion
}
