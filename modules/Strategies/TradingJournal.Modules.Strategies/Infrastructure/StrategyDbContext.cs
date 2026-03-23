using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using TradingJournal.Shared.Abstractions;

namespace TradingJournal.Modules.Strategies.Infrastructure;

internal sealed class StrategyDbContext(DbContextOptions<StrategyDbContext> options)
    : DbContext(options), IStrategyDbContext
{
    private IDbContextTransaction? _transaction;

    public DbSet<Strategy> Strategies { get; set; }

    public DbSet<StrategyTemplate> StrategyTemplates { get; set; }

    public DbSet<Backtest> Backtests { get; set; }

    public DbSet<BacktestTrade> BacktestTrades { get; set; }

    public async Task BeginTransaction()
    {
        _transaction = await Database.BeginTransactionAsync();
    }

    public async Task CommitTransaction()
    {
        if (_transaction == null) return;
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransaction()
    {
        if (_transaction == null) return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        foreach (EntityEntry<EntityBase<int>> entry in ChangeTracker.Entries<EntityBase<int>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = 0;
                    break;
                case EntityState.Modified:
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        return base.SaveChangesAsync(cancellationToken);
    }
}