namespace TradingJournal.Modules.Strategies.ViewModel;

public class TemplateDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StrategyCategory Category { get; set; }

    public string Asset { get; set; } = string.Empty;

    public string Timeframe { get; set; } = string.Empty;

    public DateTime? DateRangeStart { get; set; }

    public DateTime? DateRangeEnd { get; set; }

    public List<string> EntryIndicators { get; set; } = [];

    public List<string> ExitIndicators { get; set; } = [];

    public double RiskPerTrade { get; set; }

    public StopLossType StopLossType { get; set; }

    public double StopLossValue { get; set; }

    public TakeProfitType TakeProfitType { get; set; }

    public double TakeProfitValue { get; set; }

    public PositionSizingType PositionSizing { get; set; }

    public double PositionSizeValue { get; set; }

    public DateTime CreatedDate { get; set; }
}
