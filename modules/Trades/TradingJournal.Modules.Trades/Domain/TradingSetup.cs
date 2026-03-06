using System.ComponentModel.DataAnnotations.Schema;

namespace TradingJournal.Modules.Trades.Domain;

[Table(name: "TradingSetups", Schema = "Setups")]
public sealed class TradingSetup : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SetupStatus Status { get; set; } = SetupStatus.Draft;

    public string? Notes { get; set; }

    public ICollection<SetupStep> Steps { get; set; } = [];

    public ICollection<SetupConnection> Connections { get; set; } = [];
}
