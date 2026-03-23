namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class GetBacktestDetail
{
    internal sealed record Request(int Id) : IQuery<Result<BacktestDetailViewModel>>;

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<BacktestDetailViewModel>>
    {
        public async Task<Result<BacktestDetailViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            Backtest? backtest = await context.Backtests
                .AsNoTracking()
                .Include(b => b.Strategy)
                .Include(b => b.BacktestTrades)
                .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

            if (backtest is null)
                return Result<BacktestDetailViewModel>.Failure(Error.Create("Backtest not found."));

            BacktestDetailViewModel viewModel = new()
            {
                Id = backtest.Id,
                StrategyId = backtest.StrategyId,
                StrategyName = backtest.Strategy?.Name ?? string.Empty,
                Name = backtest.Name,
                Status = backtest.Status,
                StartDate = backtest.StartDate,
                EndDate = backtest.EndDate,
                InitialCapital = backtest.InitialCapital,
                FinalCapital = backtest.FinalCapital,
                Notes = backtest.Notes,
                TotalTrades = backtest.TotalTrades,
                WinCount = backtest.WinCount,
                LossCount = backtest.LossCount,
                WinRate = backtest.WinRate,
                TotalPnl = backtest.TotalPnl,
                ProfitFactor = backtest.ProfitFactor,
                MaxDrawdown = backtest.MaxDrawdown,
                MaxDrawdownPct = backtest.MaxDrawdownPct,
                SharpeRatio = backtest.SharpeRatio,
                AvgWin = backtest.AvgWin,
                AvgLoss = backtest.AvgLoss,
                LargestWin = backtest.LargestWin,
                LargestLoss = backtest.LargestLoss,
                CreatedDate = backtest.CreatedDate,
                Trades = backtest.BacktestTrades.Select(t => new BacktestTradeViewModel
                {
                    Id = t.Id,
                    Asset = t.Asset,
                    Position = t.Position,
                    EntryPrice = t.EntryPrice,
                    ExitPrice = t.ExitPrice,
                    StopLoss = t.StopLoss,
                    TakeProfit = t.TakeProfit,
                    EntryDate = t.EntryDate,
                    ExitDate = t.ExitDate,
                    Pnl = t.Pnl,
                    Notes = t.Notes
                }).OrderBy(t => t.EntryDate).ToList()
            };

            return Result<BacktestDetailViewModel>.Success(viewModel);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapGet("/{id:int}", async (int id, ISender sender) =>
            {
                Result<BacktestDetailViewModel> result = await sender.Send(new Request(id));

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.NotFound(result);
            })
            .Produces<Result<BacktestDetailViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get backtest detail.")
            .WithDescription("Retrieves a single backtest with all metrics and simulated trades.")
            .WithTags(Tags.Backtest);
        }
    }
}
