using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Modules.Strategies.Common.Enums;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Strategies.Domain;

[Table(name: "Backtests", Schema = "Strategies")]
public sealed class Backtest : EntityBase<int>
{
    public int StrategyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double InitialCapital { get; set; }

    public double FinalCapital { get; set; }

    public BacktestStatus Status { get; set; } = BacktestStatus.Pending;

    public string? Notes { get; set; }

    public string? DataFilePath { get; set; }

    #region Computed Metrics

    public int TotalTrades { get; set; }

    public int WinCount { get; set; }

    public int LossCount { get; set; }

    public double WinRate { get; set; }

    public double TotalPnl { get; set; }

    public double ProfitFactor { get; set; }

    public double MaxDrawdown { get; set; }

    public double MaxDrawdownPct { get; set; }

    public double SharpeRatio { get; set; }

    public double AvgWin { get; set; }

    public double AvgLoss { get; set; }

    public double LargestWin { get; set; }

    public double LargestLoss { get; set; }

    #endregion

    [ForeignKey(nameof(StrategyId))]
    public Strategy Strategy { get; set; } = null!;

    public ICollection<BacktestTrade> BacktestTrades { get; set; } = [];
}
