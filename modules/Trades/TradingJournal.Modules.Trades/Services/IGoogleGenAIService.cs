using TradingJournal.Modules.Trades.Dto;

namespace TradingJournal.Modules.Trades.Services;

public interface IGoogleGenAIService
{
    Task<TradeAnalysisResultDto?> GenerateTradingOrderSummary(int tradeHistoryId, CancellationToken cancellationToken);
}
