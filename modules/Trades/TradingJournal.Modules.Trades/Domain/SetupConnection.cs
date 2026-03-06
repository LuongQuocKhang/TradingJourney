using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "SetupConnections", Schema = "Setups")]
public sealed class SetupConnection : EntityBase<int>
{
    public int TradingSetupId { get; set; }

    public int SourceStepId { get; set; }

    public int TargetStepId { get; set; }

    public string? Label { get; set; }

    public bool IsAnimated { get; set; } = true;

    public string? Color { get; set; } = "#6366f1";

    [ForeignKey(nameof(TradingSetupId))]
    public TradingSetup? TradingSetup { get; set; }

    [ForeignKey(nameof(SourceStepId))]
    public SetupStep? SourceStep { get; set; }

    [ForeignKey(nameof(TargetStepId))]
    public SetupStep? TargetStep { get; set; }
}
