using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Stategies;

public sealed class DuplicateStrategy
{
    public class Request : ICommand<Result<int>>
    {
        public int Id { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Strategy ID must be greater than 0.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            Strategy? source = await context.Strategies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (source == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            Strategy duplicate = new()
            {
                Id = 0,
                Name = $"{source.Name} (Copy)",
                Description = source.Description,
                Type = source.Type,
                Status = StrategyStatus.Draft,
                Asset = source.Asset,
                Timeframe = source.Timeframe,
                DateRangeStart = source.DateRangeStart,
                DateRangeEnd = source.DateRangeEnd,
                EntryIndicators = source.EntryIndicators,
                ExitIndicators = source.ExitIndicators,
                RiskPerTrade = source.RiskPerTrade,
                StopLossType = source.StopLossType,
                StopLossValue = source.StopLossValue,
                TakeProfitType = source.TakeProfitType,
                TakeProfitValue = source.TakeProfitValue,
                PositionSizing = source.PositionSizing,
                PositionSizeValue = source.PositionSizeValue
            };

            await context.Strategies.AddAsync(duplicate, cancellationToken);
            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0
                ? Result<int>.Success(duplicate.Id)
                : Result<int>.Failure(Error.Create("Failed to duplicate strategy."));
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategies");

            group.MapPost("/{id}/duplicate", async ([FromRoute] int id, ISender sender) =>
            {
                Result<int> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess
                    ? Results.Created($"/api/v1/strategies/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Duplicate a strategy.")
            .WithDescription("Creates a copy of an existing strategy with Draft status.")
            .WithTags(Tags.Strategy);
        }
    }
}
