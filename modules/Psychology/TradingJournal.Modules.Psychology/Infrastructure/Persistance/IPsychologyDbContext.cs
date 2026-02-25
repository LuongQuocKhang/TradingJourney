using TradingJournal.Modules.Psychology.Domain;

namespace TradingJournal.Modules.Psychology.Infrastructure.Persistance
{
    public interface IPsychologyDbContext
    {
        public DbSet<EmotionTag> EmotionTags { get; set; }

        Task BeginTransaction();

        Task CommitTransaction();

        Task RollbackTransaction();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
