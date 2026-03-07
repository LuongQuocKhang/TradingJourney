using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Templetes;

public sealed class UpdateTemplete
{
    public record Request(
        int Id,
        string Name,
        string Description,
        StrategyCategory Category,
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
                .WithMessage("Template ID must be greater than 0.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Template name is required.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            StrategyTemplate? template = await context.StrategyTemplates
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (template == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            template.Name = request.Name;
            template.Description = request.Description;
            template.Category = request.Category;
            template.Asset = request.Asset;
            template.Timeframe = request.Timeframe;
            template.DateRangeStart = request.DateRangeStart;
            template.DateRangeEnd = request.DateRangeEnd;
            template.EntryIndicators = JsonSerializer.Serialize(request.EntryIndicators ?? []);
            template.ExitIndicators = JsonSerializer.Serialize(request.ExitIndicators ?? []);
            template.RiskPerTrade = request.RiskPerTrade;
            template.StopLossType = request.StopLossType;
            template.StopLossValue = request.StopLossValue;
            template.TakeProfitType = request.TakeProfitType;
            template.TakeProfitValue = request.TakeProfitValue;
            template.PositionSizing = request.PositionSizing;
            template.PositionSizeValue = request.PositionSizeValue;

            await context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(template.Id);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategy-templates");

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
            .WithSummary("Update an existing strategy template.")
            .WithDescription("Updates an existing strategy template by its ID.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
