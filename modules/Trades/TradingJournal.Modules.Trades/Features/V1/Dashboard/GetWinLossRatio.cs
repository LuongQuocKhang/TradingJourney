namespace TradingJournal.Modules.Trades.Features.V1.Dashboard;

public sealed class GetWinLossRatio
{
    internal sealed record Request : IQuery<Result<IReadOnlyCollection<WinLossRatioViewModel>>>;

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<IReadOnlyCollection<WinLossRatioViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<WinLossRatioViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            int userId = 0; // TODO: Get user ID from context

            List<TradeHistory>? trades = await context.TradeHistories
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId && t.Status == TradeStatus.Closed && t.Pnl.HasValue)
                .ToListAsync(cancellationToken);

            if (trades is null || trades.Count == 0)
            {
                return Result<IReadOnlyCollection<WinLossRatioViewModel>>.Failure(Error.NotFound);
            }

            int wins = trades.Count(t => t.Pnl.Value > 0);
            int losses = trades.Count(t => t.Pnl.Value < 0);

            IReadOnlyCollection<WinLossRatioViewModel> winLossRatios = [
                new WinLossRatioViewModel("Wins", wins),
                new WinLossRatioViewModel("Losses", losses)
            ];

            return Result<IReadOnlyCollection<WinLossRatioViewModel>>.Success(winLossRatios);
        }
    }

    internal sealed record WinLossRatioViewModel(string Name, double Value);

    public sealed class Endpoint() : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/dashboard");

            group.MapGet("/win-loss-ratio", async (IMediator sender) =>
            {
                Result<IReadOnlyCollection<WinLossRatioViewModel>> result = await sender.Send(new Request());

                return result.IsSuccess ? Results.Ok(result) : Results.Problem(result.Errors[0].Description);
            })
            .Produces<Result<IReadOnlyCollection<WinLossRatioViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get win/loss ratio.")
            .WithDescription("Retrieves the count of winning and losing trades for the user.")
            .WithTags(Tags.Dashboard);
        }
    }
}