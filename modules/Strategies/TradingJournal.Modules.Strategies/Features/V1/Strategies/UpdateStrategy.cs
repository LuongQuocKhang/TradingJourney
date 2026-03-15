using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Strategies;

public sealed class UpdateStrategy
{
    public record Request(
        int Id,
        string Name,
        string Description,
        StrategyType Type,
        StrategyStatus Status,
        string Asset,
        string Timeframe,
        DateTime? DateRangeStart,
        DateTime? DateRangeEnd,
        List<string> EntryIndicators,
        List<string> ExitIndicators,
        double RiskPerTrade,
        StopLossType StopLossType,
        double StopLossValue,
        TakeProfitType TakeProfitType,
        double TakeProfitValue,
        PositionSizingType PositionSizing,
        double PositionSizeValue) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Strategy ID must be greater than 0.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Strategy name is required.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            Strategy? strategy = await context.Strategies
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (strategy == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            strategy.Name = request.Name;
            strategy.Description = request.Description;
            strategy.Type = request.Type;
            strategy.Status = request.Status;
            strategy.Asset = request.Asset;
            strategy.Timeframe = request.Timeframe;
            strategy.DateRangeStart = request.DateRangeStart;
            strategy.DateRangeEnd = request.DateRangeEnd;
            strategy.EntryIndicators = JsonSerializer.Serialize(request.EntryIndicators ?? []);
            strategy.ExitIndicators = JsonSerializer.Serialize(request.ExitIndicators ?? []);
            strategy.RiskPerTrade = request.RiskPerTrade;
            strategy.StopLossType = request.StopLossType;
            strategy.StopLossValue = request.StopLossValue;
            strategy.TakeProfitType = request.TakeProfitType;
            strategy.TakeProfitValue = request.TakeProfitValue;
            strategy.PositionSizing = request.PositionSizing;
            strategy.PositionSizeValue = request.PositionSizeValue;

            await context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(strategy.Id);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategies");

            group.MapPut("/{id}", async ([FromRoute] int id, [FromBody] Request request, ISender sender) =>
            {
                Request updatedRequest = request with { Id = id };
                Result<int> result = await sender.Send(updatedRequest);

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Update an existing strategy.")
            .WithDescription("Updates an existing trading strategy by its ID.")
            .WithTags(Tags.Strategy);
        }
    }
}
