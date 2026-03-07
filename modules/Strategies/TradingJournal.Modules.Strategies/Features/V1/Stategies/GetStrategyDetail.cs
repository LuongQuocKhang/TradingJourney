using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Stategies;

public class GetStrategyDetail
{
    public class Request : IQuery<Result<StrategyDetailViewModel>>
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

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<StrategyDetailViewModel>>
    {
        public async Task<Result<StrategyDetailViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            Strategy? strategy = await context.Strategies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (strategy == null)
            {
                return Result<StrategyDetailViewModel>.Failure(Error.NotFound);
            }

            StrategyDetailViewModel viewModel = new()
            {
                Id = strategy.Id,
                Name = strategy.Name,
                Description = strategy.Description,
                Type = strategy.Type,
                Status = strategy.Status,
                Asset = strategy.Asset,
                Timeframe = strategy.Timeframe,
                DateRangeStart = strategy.DateRangeStart,
                DateRangeEnd = strategy.DateRangeEnd,
                EntryIndicators = DeserializeIndicators(strategy.EntryIndicators),
                ExitIndicators = DeserializeIndicators(strategy.ExitIndicators),
                RiskPerTrade = strategy.RiskPerTrade,
                StopLossType = strategy.StopLossType,
                StopLossValue = strategy.StopLossValue,
                TakeProfitType = strategy.TakeProfitType,
                TakeProfitValue = strategy.TakeProfitValue,
                PositionSizing = strategy.PositionSizing,
                PositionSizeValue = strategy.PositionSizeValue,
                CreatedDate = strategy.CreatedDate
            };

            return Result<StrategyDetailViewModel>.Success(viewModel);
        }

        private static List<string> DeserializeIndicators(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategies");

            group.MapGet("/{id}", async ([FromRoute] int id, ISender sender) =>
            {
                Result<StrategyDetailViewModel> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<StrategyDetailViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get strategy details by ID.")
            .WithDescription("Retrieves a single trading strategy with all configuration details.")
            .WithTags(Tags.Strategy);
        }
    }
}
