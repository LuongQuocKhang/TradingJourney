namespace TradingJournal.Modules.Strategies.Features.V1.Backtests;

public sealed class CreateBacktest
{
    public record Request(
        int StrategyId,
        string Name,
        DateTime StartDate,
        DateTime EndDate,
        double InitialCapital,
        string? Notes) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.StrategyId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("StrategyId is required.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Backtest name is required.");

            RuleFor(x => x.InitialCapital)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Initial capital must be greater than 0.");

            RuleFor(x => x.EndDate)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(x => x.StartDate).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("End date must be after start date.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                Strategy? strategy = await context.Strategies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.StrategyId, cancellationToken);

                if (strategy is null)
                    return Result<int>.Failure(Error.Create("Strategy not found."));

                Backtest backtest = new()
                {
                    Id = 0,
                    StrategyId = request.StrategyId,
                    Name = request.Name,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    InitialCapital = request.InitialCapital,
                    FinalCapital = request.InitialCapital,
                    Status = BacktestStatus.Pending,
                    Notes = request.Notes
                };

                await context.Backtests.AddAsync(backtest, cancellationToken);
                int insertedRow = await context.SaveChangesAsync(cancellationToken);

                return insertedRow > 0
                    ? Result<int>.Success(backtest.Id)
                    : Result<int>.Failure(Error.Create("Failed to create backtest."));
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(Error.Create(ex.Message));
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/backtests");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) =>
            {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/backtests/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new backtest.")
            .WithDescription("Creates a new backtest linked to a strategy.")
            .WithTags(Tags.Backtest);
        }
    }
}
