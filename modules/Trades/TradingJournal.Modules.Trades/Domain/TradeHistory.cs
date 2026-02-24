using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradeHistorys", Schema = "Trades")]
public sealed class TradeHistory : EntityBase<int>
{
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

    public ICollection<TradeScreenShot> Screenshots { get; set; } = [];

    public ICollection<EmotionTag> EmotionTags { get; set; } = [];

    public ICollection<PretradeChecklist> PretradeChecklists { get; set; } = [];

    public TradingSession? TradingSession { get; set; }

    public RiskGuardrail? RiskGuardrail { get; set; }
}
