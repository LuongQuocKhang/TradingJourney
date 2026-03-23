using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Strategies.Domain;

[Table(name: "BacktestTrades", Schema = "Strategies")]
public sealed class BacktestTrade : EntityBase<int>
{
    public int BacktestId { get; set; }

    public string Asset { get; set; } = string.Empty;

    /// <summary>
    /// 0 = Long, 1 = Short
    /// </summary>
    public int Position { get; set; }

    public double EntryPrice { get; set; }

    public double ExitPrice { get; set; }

    public double StopLoss { get; set; }

    public double TakeProfit { get; set; }

    public DateTime EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    public double Pnl { get; set; }

    public string? Notes { get; set; }

    [ForeignKey(nameof(BacktestId))]
    public Backtest Backtest { get; set; } = null!;
}
