using Microsoft.EntityFrameworkCore;
using TradingJournal.Modules.Strategies.Domain;

namespace TradingJournal.Modules.Strategies.Infrastructure;

public interface IStrategyDbContext
{
    DbSet<Strategy> Strategies { get; set; }

    DbSet<StrategyTemplate> StrategyTemplates { get; set; }

    DbSet<Backtest> Backtests { get; set; }

    DbSet<BacktestTrade> BacktestTrades { get; set; }

    Task BeginTransaction();

    Task CommitTransaction();

    Task RollbackTransaction();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
