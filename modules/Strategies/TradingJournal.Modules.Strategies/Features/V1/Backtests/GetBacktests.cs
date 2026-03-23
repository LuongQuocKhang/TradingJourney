namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class GetBacktests
{
    internal sealed record Request(int? StrategyId) : IQuery<Result<List<BacktestViewModel>>>;

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<List<BacktestViewModel>>>
    {
        public async Task<Result<List<BacktestViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            IQueryable<Backtest> query = context.Backtests
                .AsNoTracking()
                .Include(b => b.Strategy);

            if (request.StrategyId.HasValue)
            {
                query = query.Where(b => b.StrategyId == request.StrategyId.Value);
            }

            List<Backtest> backtests = await query
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync(cancellationToken);

            List<BacktestViewModel> viewModels = backtests.Select(b => new BacktestViewModel
            {
                Id = b.Id,
                StrategyId = b.StrategyId,
                StrategyName = b.Strategy?.Name ?? string.Empty,
                Name = b.Name,
                Status = b.Status,
                InitialCapital = b.InitialCapital,
                FinalCapital = b.FinalCapital,
                TotalTrades = b.TotalTrades,
                WinRate = b.WinRate,
                TotalPnl = b.TotalPnl,
                CreatedDate = b.CreatedDate
            }).ToList();

            return Result<List<BacktestViewModel>>.Success(viewModels);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapGet("/", async (int? strategyId, ISender sender) =>
            {
                Result<List<BacktestViewModel>> result = await sender.Send(new Request(strategyId));

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<List<BacktestViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Get all backtests.")
            .WithDescription("Retrieves all backtests, optionally filtered by strategy.")
            .WithTags(Tags.Backtest);
        }
    }
}
