using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradeHistorys", Schema = "Trades")]
public sealed class TradeHistory : EntityBase<int>
{
    public string Asset { get; set; } = string.Empty;
    
    public PositionType Position { get; set; }

    public double EntryPrice { get; set; }

    public double TargetTier1 { get; set; }

    public double? TargetTier2 { get; set; }

    public double? TargetTier3 { get; set; }

    public double StopLoss { get; set; }

    public string? Notes { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public TradeStatus Status { get; set; }

    public double? ExitPrice { get; set; }

    public double? Pnl { get; set; }

    public DateTime? ClosedDate { get; set; }

    public ConfidenceLevel ConfidenceLevel { get; set; }

    public string? PsychologyNotes { get; set; }

    public int? TradingSessionId { get; set; }

    public int? RiskGuardrailId { get; set; }

    public ICollection<TradeScreenShot> Screenshots { get; set; } = [];

    public ICollection<TradeEmotionTag>? TradeEmotionTags { get; set; } = [];

    public ICollection<TradeHistoryChecklist> PretradeChecklists { get; set; } = [];

    [ForeignKey(nameof(TradingSessionId))]
    public TradeHistorySession? TradeHistorySession { get; set; }

    [ForeignKey(nameof(RiskGuardrailId))]
    public RiskGuardrail? RiskGuardrail { get; set; }
}
