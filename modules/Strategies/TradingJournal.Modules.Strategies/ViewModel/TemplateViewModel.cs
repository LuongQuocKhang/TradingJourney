namespace TradingJournal.Modules.Strategies.ViewModel;

public class TemplateViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StrategyCategory Category { get; set; }

    public string Asset { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public List<string> EntryIndicators { get; set; } = [];

    public double RiskPerTrade { get; set; }

    public DateTime CreatedDate { get; set; }
}
