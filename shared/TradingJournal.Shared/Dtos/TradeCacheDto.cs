namespace TradingJournal.Shared.Dtos;

public class TradeCacheDto
{
    public int Id { get; set; }
    public string Asset { get; set; } = string.Empty;
    public decimal? Pnl { get; set; }
    public DateTime? ClosedDate { get; set; }
    public List<int>? EmotionTags { get; set; }
}
