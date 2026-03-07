namespace TradingJournal.Modules.Strategies.ViewModel;

public class StrategyViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StrategyType Type { get; set; }

    public StrategyStatus Status { get; set; }

    public string Asset { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public List<string> EntryIndicators { get; set; } = [];

    public List<string> ExitIndicators { get; set; } = [];

    public double RiskPerTrade { get; set; }

    public DateTime CreatedDate { get; set; }
}
