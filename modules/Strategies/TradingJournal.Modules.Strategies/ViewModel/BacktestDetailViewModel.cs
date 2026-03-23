namespace TradingJournal.Modules.Strategies.ViewModel;

public class BacktestDetailViewModel
{
    public int Id { get; set; }

    public int StrategyId { get; set; }

    public string StrategyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public BacktestStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double InitialCapital { get; set; }

    public double FinalCapital { get; set; }

    public string? Notes { get; set; }

    #region Metrics

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

    public List<BacktestTradeViewModel> Trades { get; set; } = [];

    public DateTime CreatedDate { get; set; }
}

public class BacktestTradeViewModel
{
    public int Id { get; set; }

    public string Asset { get; set; } = string.Empty;

    public int Position { get; set; }

    public double EntryPrice { get; set; }

    public double ExitPrice { get; set; }

    public double StopLoss { get; set; }

    public double TakeProfit { get; set; }

    public DateTime EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    public double Pnl { get; set; }

    public string? Notes { get; set; }
}
