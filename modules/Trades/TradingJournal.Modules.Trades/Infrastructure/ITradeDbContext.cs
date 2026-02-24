using Microsoft.EntityFrameworkCore;
using TradingJournal.Modules.Trades.Domain;

namespace TradingJournal.Modules.Trades.Infrastructure;

public interface ITradeDbContext
{
    DbSet<TradeHistory> TradeHistories { get; set; }

    DbSet<EmotionTag> EmotionTags { get; set; }
    
    DbSet<PretradeChecklist> PretradeChecklists { get; set; }

    DbSet<RiskGuardrail> RiskGuardrails { get; set; }

    DbSet<TradeScreenShot> TradeScreenShots { get; set; }

    DbSet<TradingSession> TradingSessions { get; set; }

    Task BeginTransaction();

    Task CommitTransaction();

    Task RollbackTransaction();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
