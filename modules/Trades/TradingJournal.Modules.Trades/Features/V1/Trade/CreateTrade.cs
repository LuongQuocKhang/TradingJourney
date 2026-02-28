using TradingJournal.Modules.Trades.Dto;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public sealed class CreateTrade
{
    public record Request(string Asset,
        PositionType Position,
        double EntryPrice,
        double TargetTier1,
        double? TargetTier2,
        double? TargetTier3,
        double StopLoss,
        string Notes,
        DateTime Date,
        TradeStatus Status,
        double? ExitPrice,
        double? Pnl,
        DateTime? ClosedDate,
        List<string>? Screenshots,
        List<int>? TradeTechnicalAnalysisTags,
        List<int>? EmotionTags,
        ConfidenceLevel ConfidenceLevel,
        string? PsychologyNotes,
        List<int> TradeHistoryChecklists,
        int TradingSession,
        RiskGuardrailsDto? RiskGuardrail) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Asset)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Asset cannot be null.");

            RuleFor(x => x.Position)
                .Cascade(CascadeMode.Stop)
                .Must(pos => Enum.IsDefined(pos))
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Position must be a valid TradeEnum value.");

            RuleFor(x => x.EntryPrice)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("EntryPrice must be greater than 0.");

            RuleFor(x => x.TargetTier1)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("First Target Tier must be greater than 0.");

            RuleFor(x => x.StopLoss)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Stop Loss must be entered and greater than 0.");

            RuleFor(x => x.Notes)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Must enter notes ( analysis of the trade ).");

            RuleFor(x => x.Date)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Date of the trade must be entered.");

            RuleFor(x => x.Status)
                .Cascade(CascadeMode.Stop)
                .Must(status => Enum.IsDefined(status))
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Status must be a valid TradeStatus value.");

            RuleFor(x => x.TradeHistoryChecklists)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Pretrade checklists must be entered.")
                .Must(checklistIds => checklistIds != null && checklistIds.Count > 0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("At least one pretrade checklist must be provided.");

            RuleFor(x => x.TradingSession)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Trading Session must be entered and greater than 0.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            TradeHistory tradeHistory = request.Adapt<TradeHistory>();

            await context.TradeHistories.AddAsync(tradeHistory, cancellationToken);

            await context.TradeHistoryChecklists.AddRangeAsync(request.TradeHistoryChecklists.Select(checklistId => new TradeHistoryChecklist
            {
                Id = 0,
                PretradeChecklistId = checklistId,
                TradeHistory = tradeHistory
            }), cancellationToken);

            await context.TradeEmotionTags.AddRangeAsync(request.EmotionTags?.Select(tagId => new TradeEmotionTag
            {
                Id = 0,
                EmotionTagId = tagId,
                TradeHistory = tradeHistory
            }) ?? [], cancellationToken);

            await context.TradeScreenShots.AddRangeAsync(request.Screenshots?.Select(screenshot => new TradeScreenShot
            {
                Id = 0,
                Url = screenshot,
                TradeHistory = tradeHistory
            }) ?? [], cancellationToken);

            await context.TradeHistorySessions.AddAsync(new TradeHistorySession
            {
                Id = 0,
                TradeHistory = tradeHistory,
                TradingSessionId = request.TradingSession
            }, cancellationToken);

            await context.TradeTechnicalAnalysisTags.AddRangeAsync(request.TradeTechnicalAnalysisTags?.Select(tagId => new TradeTechnicalAnalysisTag
            {
                Id = 0,
                TechnicalAnalysisId = tagId,
                TradeHistory = tradeHistory
            }) ?? [], cancellationToken);

            if (request.RiskGuardrail != null)
            {
                tradeHistory.RiskGuardrail = request.RiskGuardrail.Adapt<RiskGuardrail>();
            }

            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0 ? Result<int>.Success(tradeHistory.Id)
                : Result<int>.Failure(Error.Create("Failed to create trade history."));
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trade-histories");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) => {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess ? Results.Created($"/api/v1/trade-histories/{result.Value}", result) 
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new trade history.")
            .WithDescription("Creates a new trade history with the given details.") 
            .WithTags(Tags.TradeHistory);
        }
    }
}
