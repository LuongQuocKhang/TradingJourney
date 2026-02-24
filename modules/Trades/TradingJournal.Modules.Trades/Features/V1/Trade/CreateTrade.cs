using FluentValidation.Results;
using System.Net;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Modules.Trades.Dto;
using TradingJournal.Modules.Trades.Infrastructure;
using Mapster;
using TradingJournal.Modules.Trades.Domain;
using TradingJournal.Modules.Trades.Common.Constants;
using MediatR;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public static class CreateTrade
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
        List<TradeScreenShotDto>? Screenshots,
        List<int>? EmotionTags,
        ConfidenceLevel ConfidenceLevel,
        string? PsychologyNotes,
        List<int> PretradeChecklist,
        int TradingSession,
        RiskGuardrailsDto RiskGuardrails) : ICommand<Result<int>>;

    public class Validator : AbstractValidator<Request>
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

            RuleFor(x => x.PretradeChecklist)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Pretrade checklist must be entered.");

             RuleFor(x => x.TradingSession)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Trading Session must be entered and greater than 0.");
        }
    }

    public class Handler(ITradeDbContext context) : IRequestHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            TradeHistory tradeHistory = request.Adapt<TradeHistory>();

            context.TradeHistories.Add(tradeHistory);

            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0 ? Result<int>.Success(tradeHistory.Id)
                : Result<int>.Failure(Error.Create("Failed to create trade history."));
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trades");

            group.MapPost("/create", async ([FromBody] Request request, ISender sender) => {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess ? Results.Created() 
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new trade history.")
            .WithDescription("Creates a new trade history with the given details.") 
            .WithTags(Tags.Trades);
        }
    }
}
