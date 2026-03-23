namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class DeleteBacktest
{
    internal sealed record Request(int Id) : ICommand<Result<bool>>;

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                await context.BeginTransaction();

                Backtest? backtest = await context.Backtests
                    .Include(b => b.BacktestTrades)
                    .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

                if (backtest is null)
                    return Result<bool>.Failure(Error.Create("Backtest not found."));

                // Remove child trades first
                if (backtest.BacktestTrades.Count > 0)
                {
                    context.BacktestTrades.RemoveRange(backtest.BacktestTrades);
                }

                context.Backtests.Remove(backtest);

                await context.SaveChangesAsync(cancellationToken);
                await context.CommitTransaction();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await context.RollbackTransaction();
                return Result<bool>.Failure(Error.Create(ex.Message));
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapDelete("/{id:int}", async (int id, ISender sender) =>
            {
                Result<bool> result = await sender.Send(new Request(id));

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.NotFound(result);
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Delete a backtest.")
            .WithDescription("Deletes a backtest and all its simulated trades.")
            .WithTags(Tags.Backtest);
        }
    }
}
