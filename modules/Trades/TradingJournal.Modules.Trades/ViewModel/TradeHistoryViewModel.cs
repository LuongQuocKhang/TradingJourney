using TradingJournal.Modules.Trades.Dto;

namespace TradingJournal.Modules.Trades.ViewModel;

public class TradeHistoryViewModel
{
    public int Id { get; set; }

    public string Asset { get; set; } = string.Empty;

    public PositionType Position { get; set; }

    public double EntryPrice { get; set; }

    public double TargetTier1 { get; set; }

    public double? TargetTier2 { get; set; }

    public double? TargetTier3 { get; set; }

    public double StopLoss { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public TradeStatus Status { get; set; }

    public double? ExitPrice { get; set; }

    public double? Pnl { get; set; }

    public DateTime? ClosedDate { get; set; }

    public List<TradeScreenShotDto>? Screenshots { get; set; }

    public List<int>? EmotionTags { get; set; }

    public ConfidenceLevel ConfidenceLevel { get; set; }

    public string? PsychologyNotes { get; set; }

    public List<int> PretradeChecklist { get; set; } = [];

    public int TradingSession { get; set; }

    public RiskGuardrailsDto RiskGuardrails { get; set; }
}