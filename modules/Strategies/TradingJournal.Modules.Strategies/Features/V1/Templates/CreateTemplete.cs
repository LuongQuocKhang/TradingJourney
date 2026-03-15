using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Templates;

public sealed class CreateTemplete
{
    public record Request(
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
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Template name is required.");

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
                StrategyTemplate template = new()
                {
                    Id = 0,
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
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

                await context.StrategyTemplates.AddAsync(template, cancellationToken);
                int insertedRow = await context.SaveChangesAsync(cancellationToken);

                return insertedRow > 0
                    ? Result<int>.Success(template.Id)
                    : Result<int>.Failure(Error.Create("Failed to create template."));
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
            RouteGroupBuilder group = app.MapGroup("api/v1/strategy-templates");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) =>
            {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/strategy-templates/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new strategy template.")
            .WithDescription("Creates a new strategy template with the given configuration.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
