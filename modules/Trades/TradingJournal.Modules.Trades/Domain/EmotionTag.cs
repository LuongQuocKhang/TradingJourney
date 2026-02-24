using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "EmotionTags", Schema = "Trades")]
public sealed class EmotionTag : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;

    public PsychologyType PsychologyType { get; set; }
}
