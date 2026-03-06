namespace TradingJournal.Modules.Trades.ViewModel;

public sealed class SetupViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SetupStatus Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
