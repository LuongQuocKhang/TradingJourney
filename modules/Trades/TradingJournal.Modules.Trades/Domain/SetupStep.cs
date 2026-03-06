using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "SetupSteps", Schema = "Setups")]
public sealed class SetupStep : EntityBase<int>
{
    public int TradingSetupId { get; set; }

    public int StepNumber { get; set; }

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string NodeType { get; set; } = "setupNode";

    public string? Color { get; set; } = "#6366f1";

    /// <summary>
    /// X position on the flowchart canvas.
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Y position on the flowchart canvas.
    /// </summary>
    public double PositionY { get; set; }

    [ForeignKey(nameof(TradingSetupId))]
    public TradingSetup? TradingSetup { get; set; }
}
