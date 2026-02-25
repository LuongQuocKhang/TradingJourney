namespace TradingJournal.Modules.Trades.ViewModel;

public class TradeHistoryViewModel
{
    public int Id { get; set; }

    public string Asset { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public double EntryPrice { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = string.Empty;

    public double? ExitPrice { get; set; }

    public double? Pnl { get; set; }

    public DateTime? ClosedDate { get; set; }

    public List<string>? EmotionTags { get; set; }
}