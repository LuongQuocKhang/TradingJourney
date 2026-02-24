using System.ComponentModel.DataAnnotations.Schema;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "EmotionTags", Schema = "Trades")]
public sealed class EmotionTag : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;

    public PsychologyType PsychologyType { get; set; }
}
