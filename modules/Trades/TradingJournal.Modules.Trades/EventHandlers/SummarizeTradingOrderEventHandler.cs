using Microsoft.Extensions.DependencyInjection;
using TradingJournal.Modules.Trades.Dto;
using TradingJournal.Modules.Trades.Events;
using TradingJournal.Modules.Trades.Services;

namespace TradingJournal.Modules.Trades.EventHandlers;

internal sealed class SummarizeTradingOrderEventHandler(IServiceScopeFactory serviceScopeFactory, ITradeDbContext context) : INotificationHandler<SummarizeTradingOrderEvent>
{
    public async Task Handle(SummarizeTradingOrderEvent notification, CancellationToken cancellationToken)
    {
        using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

        IGoogleGenAIService googleGenAiService = scope.ServiceProvider.GetRequiredService<IGoogleGenAIService>();

        TradeAnalysisResultDto? result = await googleGenAiService.GenerateTradingOrderSummary(notification.TradeHistoryId, cancellationToken) ?? throw new AggregateException($"Failed to generate trading order summary for TradeHistoryId: {notification.TradeHistoryId}");

        // Store to DB

        await context.TradingSummaries.AddAsync(new TradingSummary()
        {
            Id = 0,
            TradeId = notification.TradeHistoryId,
            ExecutiveSummary = result?.ExecutiveSummary ?? string.Empty,
            TechnicalInsights = result?.TechnicalInsights ?? string.Empty,
            PsychologyAnalysis = result?.PsychologyAnalysis ?? string.Empty,
            CriticalMistakes = new CriticalMistakes()
            {
                Psychological = result?.CriticalMistakes?.Psychological ?? [],
                Technical = result?.CriticalMistakes?.Technical ?? [],
            },
        }, cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        await Task.CompletedTask;
    }
}
