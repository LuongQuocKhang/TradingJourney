using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using TradingJournal.Modules.Psychology.Domain;

namespace TradingJournal.Modules.Psychology.Infrastructure.Persistance;

internal sealed class PsychologyDbContext(DbContextOptions<PsychologyDbContext> options)
    : DbContext(options), IPsychologyDbContext
{
    private IDbContextTransaction? _transaction;

    public DbSet<EmotionTag> EmotionTags { get; set; }

    public DbSet<PsychologyJournal> PsychologyJournals { get; set; }

    public DbSet<PsychologyJournalEmotion> PsychologyJournalEmotions { get; set; }

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