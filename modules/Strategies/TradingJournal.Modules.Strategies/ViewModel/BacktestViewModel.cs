namespace TradingJournal.Modules.Strategies.ViewModel;

public class BacktestViewModel
{
    public int Id { get; set; }

    public int StrategyId { get; set; }

    public string StrategyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public BacktestStatus Status { get; set; }

    public double InitialCapital { get; set; }

    public double FinalCapital { get; set; }

    public int TotalTrades { get; set; }

    public double WinRate { get; set; }

    public double TotalPnl { get; set; }

    public DateTime CreatedDate { get; set; }
}
