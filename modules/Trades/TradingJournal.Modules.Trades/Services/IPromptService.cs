namespace TradingJournal.Modules.Trades.Services;

public interface IPromptService
{
    public Task<string> GetTradingOrderSummary();
}
