using Mapster;
using TradingJournal.Modules.Trades.Dto;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public sealed class UpdateTrade
{
    public record Request(
        int Id,
        string Asset,
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
        int TradingZoneId,
        int? TradingSessionId,
        RiskGuardrailsDto? RiskGuardrail) : ICommand<Result<bool>>;

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

            RuleFor(x => x.TradingZoneId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Trading Zone must be entered and greater than 0.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {

            try
            {
                TradeHistory? tradeHistory = await context.TradeHistories
                    .Include(th => th.TradeScreenShots)
                    .Include(th => th.TradeEmotionTags)
                    .Include(th => th.TradeChecklists)
                    .Include(x => x.TradeTechnicalAnalysisTags)
                    .Include(th => th.RiskGuardrail)
                    .FirstOrDefaultAsync(th => th.Id == request.Id, cancellationToken: cancellationToken);

                if (tradeHistory == null)
                {
                    return Result<bool>.Failure(Error.NotFound);
                }

                tradeHistory.Asset = request.Asset;
                tradeHistory.Position = request.Position;
                tradeHistory.EntryPrice = request.EntryPrice;
                tradeHistory.TargetTier1 = request.TargetTier1;
                tradeHistory.TargetTier2 = request.TargetTier2;
                tradeHistory.TargetTier3 = request.TargetTier3;
                tradeHistory.StopLoss = request.StopLoss;
                tradeHistory.Notes = request.Notes;
                tradeHistory.Date = request.Date;
                tradeHistory.Status = request.Status;
                tradeHistory.ExitPrice = request.ExitPrice;
                tradeHistory.Pnl = request.Pnl;
                tradeHistory.ClosedDate = request.ClosedDate;
                tradeHistory.ConfidenceLevel = request.ConfidenceLevel;
                tradeHistory.PsychologyNotes = request.PsychologyNotes;
                tradeHistory.TradingZoneId = request.TradingZoneId;
                tradeHistory.TradingSessionId = request.TradingSessionId;

                #region remove all existing screenshots, emotion tags, pretrade checklists, and trading session associations
                context.TradeScreenShots.RemoveRange(tradeHistory.TradeScreenShots);
                context.TradeEmotionTags.RemoveRange(tradeHistory.TradeEmotionTags ?? []);
                context.TradeHistoryChecklist.RemoveRange(tradeHistory.TradeChecklists);
                context.TradeTechnicalAnalysisTags.RemoveRange(tradeHistory.TradeTechnicalAnalysisTags ?? []);

                await context.TradeHistoryChecklist.AddRangeAsync(request.TradeHistoryChecklists.Select(checklistId => new TradeHistoryChecklist
                {
                    Id = 0,
                    TradeHistoryId = tradeHistory.Id,
                    PretradeChecklistId = checklistId
                }), cancellationToken);

                await context.TradeEmotionTags.AddRangeAsync(request.EmotionTags?.Select(tagId => new TradeEmotionTag
                {
                    Id = 0,
                    TradeHistoryId = tradeHistory.Id,
                    EmotionTagId = tagId
                }) ?? [], cancellationToken);

                await context.TradeScreenShots.AddRangeAsync(request.Screenshots?.Select(screenshot => new TradeScreenShot
                {
                    Id = 0,
                    TradeHistoryId = tradeHistory.Id,
                    Url = screenshot
                }) ?? [], cancellationToken);

                await context.TradeTechnicalAnalysisTags.AddRangeAsync(request.TradeTechnicalAnalysisTags?.Select(tagId => new TradeTechnicalAnalysisTag
                {
                    Id = 0,
                    TradeHistoryId = tradeHistory.Id,
                    TechnicalAnalysisId = tagId
                }) ?? [], cancellationToken);
                #endregion

                #region update or create risk guardrail association

                if (request.RiskGuardrail != null)
                {
                    tradeHistory.RiskGuardrail = request.RiskGuardrail.Adapt<RiskGuardrail>();
                }
                else
                {
                    tradeHistory.RiskGuardrail = null;
                }

                #endregion

                await context.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch
            {
                throw;
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trade-histories");

            group.MapPut("/", async ([FromBody] Request request, ISender sender) => {
                Result<bool> result = await sender.Send(request);

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Update an existing trade history.")
            .WithDescription("Updates an existing trade history with the given details.")
            .WithTags(Tags.TradeHistory);
        }
    }
}