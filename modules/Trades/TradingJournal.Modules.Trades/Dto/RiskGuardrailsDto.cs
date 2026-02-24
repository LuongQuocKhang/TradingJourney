namespace TradingJournal.Modules.Trades.Dto;

public sealed record RiskGuardrailsDto(double? AccountEquity,
    double? RiskPercentage,
    double? MaxDailyLoss,
    double? TakeProfit,
    double? PositionSize);