using System.Text.Json;
using Mapster;

namespace TradingJournal.Modules.Strategies.Features.V1.Stategies;

public sealed class CreateStrategy
{
    public record Request(
        string Name,
        string Description,
        StrategyType Type,
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
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Strategy name is required.");

            RuleFor(x => x.Asset)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Asset is required.");

            RuleFor(x => x.Timeframe)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Timeframe is required.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                Strategy strategy = new()
                {
                    Id = 0,
                    Name = request.Name,
                    Description = request.Description,
                    Type = request.Type,
                    Status = StrategyStatus.Draft,
                    Asset = request.Asset,
                    Timeframe = request.Timeframe,
                    DateRangeStart = request.DateRangeStart,
                    DateRangeEnd = request.DateRangeEnd,
                    EntryIndicators = JsonSerializer.Serialize(request.EntryIndicators ?? []),
                    ExitIndicators = JsonSerializer.Serialize(request.ExitIndicators ?? []),
                    RiskPerTrade = request.RiskPerTrade,
                    StopLossType = request.StopLossType,
                    StopLossValue = request.StopLossValue,
                    TakeProfitType = request.TakeProfitType,
                    TakeProfitValue = request.TakeProfitValue,
                    PositionSizing = request.PositionSizing,
                    PositionSizeValue = request.PositionSizeValue
                };

                await context.Strategies.AddAsync(strategy, cancellationToken);
                int insertedRow = await context.SaveChangesAsync(cancellationToken);

                return insertedRow > 0
                    ? Result<int>.Success(strategy.Id)
                    : Result<int>.Failure(Error.Create("Failed to create strategy."));
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
            RouteGroupBuilder group = app.MapGroup("api/v1/strategies");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) =>
            {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/strategies/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new strategy.")
            .WithDescription("Creates a new trading strategy with the given configuration.")
            .WithTags(Tags.Strategy);
        }
    }
}
