namespace TradingJournal.Modules.Strategies.Features.V1.Templetes;

public sealed class LoadAndBackTestTemplete
{
    public class Request : ICommand<Result<int>>
    {
        public int TemplateId { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Template ID must be greater than 0.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            StrategyTemplate? template = await context.StrategyTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.TemplateId, cancellationToken);

            if (template == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            Strategy strategy = new()
            {
                Id = 0,
                Name = template.Name,
                Description = template.Description,
                Type = StrategyType.Backtest,
                Status = StrategyStatus.Draft,
                Asset = template.Asset,
                Timeframe = template.Timeframe,
                DateRangeStart = template.DateRangeStart,
                DateRangeEnd = template.DateRangeEnd,
                EntryIndicators = template.EntryIndicators,
                ExitIndicators = template.ExitIndicators,
                RiskPerTrade = template.RiskPerTrade,
                StopLossType = template.StopLossType,
                StopLossValue = template.StopLossValue,
                TakeProfitType = template.TakeProfitType,
                TakeProfitValue = template.TakeProfitValue,
                PositionSizing = template.PositionSizing,
                PositionSizeValue = template.PositionSizeValue
            };

            await context.Strategies.AddAsync(strategy, cancellationToken);
            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0
                ? Result<int>.Success(strategy.Id)
                : Result<int>.Failure(Error.Create("Failed to create strategy from template."));
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategy-templates");

            group.MapPost("/{id}/load-and-backtest", async ([FromRoute] int id, ISender sender) =>
            {
                Result<int> result = await sender.Send(new Request { TemplateId = id });

                return result.IsSuccess
                    ? Results.Created($"/api/v1/strategies/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Load a template and create a strategy for backtesting.")
            .WithDescription("Creates a new strategy from a template's configuration with Draft status.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
