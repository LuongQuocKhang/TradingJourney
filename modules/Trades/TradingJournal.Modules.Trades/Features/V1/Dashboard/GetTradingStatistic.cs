namespace TradingJournal.Modules.Trades.Features.V1.Dashboard;

public sealed class GetTradingStatistic
{
    internal sealed record Request : IQuery<Result<TradingStatisticViewModel>>;

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<TradingStatisticViewModel>>
    {
        public async Task<Result<TradingStatisticViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            int userId = 0; // TODO: Get user ID from context

            List<TradeHistory>? trades = await context.TradeHistories
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId)
                .ToListAsync(cancellationToken);

            if (trades is null || trades.Count == 0)
            {
                return Result<TradingStatisticViewModel>.Failure(Error.NotFound);
            }

            List<TradeHistory> closedTrades = trades.Where(t => t.Status == TradeStatus.Closed).ToList();

            double totalPnL = closedTrades.Where(t => t.Pnl.HasValue).Sum(t => t.Pnl.Value);

            double winRate = closedTrades.Count(t => t.Pnl.HasValue && t.Pnl.Value > 0) / (double)closedTrades.Count(t => t.Pnl.HasValue) * 100;
            
            int totalTrades = trades.Count;

            int openPositions = trades.Where(x => x.Status == TradeStatus.Open).Count(t => !t.Pnl.HasValue);

            TradingStatisticViewModel statistic = new()
            {
                TotalPnL = totalPnL,
                WinRate = winRate,
                TotalTrades = totalTrades,
                OpenPositions = openPositions
            };

            return Result<TradingStatisticViewModel>.Success(statistic);
        }
    }

    public sealed class Endpoint() : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/dashboard");

            group.MapGet("/statistics", async (IMediator sender) =>
            {
                Result<TradingStatisticViewModel> result = await sender.Send(new Request());

                return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Errors[0].Description);
            })
            .Produces<Result<TradingStatisticViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get trading statistics.")
            .WithDescription("Retrieves the trading statistics including total PnL, win rate, total trades, and open positions.")
            .WithTags(Tags.Dashboard);
        }
    }
}