using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using TradingJournal.Modules.Trades.Domain;

namespace TradingJournal.Modules.Trades.Infrastructure;

internal sealed class TradeDbContext(DbContextOptions<TradeDbContext> options)
    : DbContext(options), ITradeDbContext
{
    private IDbContextTransaction? _transaction;

    public DbSet<TradeHistory> TradeHistories { get; set; }

    public DbSet<EmotionTag> EmotionTags { get; set; }

    public DbSet<PretradeChecklist> PretradeChecklists { get; set; }

    public DbSet<RiskGuardrail> RiskGuardrails { get; set; }

    public DbSet<TradeScreenShot> TradeScreenShots { get; set; }

    public DbSet<TradingSession> TradingSessions { get; set; }

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
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.EnableSensitiveDataLogging();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        foreach (EntityEntry<EntityBase<int>> entry in ChangeTracker.Entries<EntityBase<int>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow.AddHours(7);
                    entry.Entity.CreatedBy = 0;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = DateTime.UtcNow.AddHours(7);
                    entry.Entity.UpdatedBy = 0;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
