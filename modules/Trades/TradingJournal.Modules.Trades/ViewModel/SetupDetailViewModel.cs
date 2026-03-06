namespace TradingJournal.Modules.Trades.ViewModel;

public sealed class SetupDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SetupStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public List<SetupStepViewModel> Steps { get; set; } = [];

    public List<SetupConnectionViewModel> Connections { get; set; } = [];
}

public sealed class SetupStepViewModel
{
    public int Id { get; set; }

    public int StepNumber { get; set; }

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string NodeType { get; set; } = "setupNode";

    public string? Color { get; set; }

    public double PositionX { get; set; }

    public double PositionY { get; set; }
}

public sealed class SetupConnectionViewModel
{
    public int Id { get; set; }

    public int SourceStepId { get; set; }

    public int TargetStepId { get; set; }

    public string? Label { get; set; }

    public bool IsAnimated { get; set; }

    public string? Color { get; set; }
}
