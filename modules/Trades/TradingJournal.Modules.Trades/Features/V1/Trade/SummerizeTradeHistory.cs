using TradingJournal.Modules.Trades.Dto;
using TradingJournal.Modules.Trades.Services;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public sealed class SummerizeTradeHistory
{
    public record Request(int TradeId) : IQuery<Result<bool>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TradeId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(nameof(HttpStatusCode.BadRequest))
                .WithMessage("TradeId must be greater than 0.");
        }
    }

    internal sealed class Handler(ITradeDbContext context, IGoogleGenAIService googleGenAIService) : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            TradeAnalysisResultDto? result = await googleGenAIService.GenerateTradingOrderSummary(request.TradeId, cancellationToken);

            if (result is null)
            {
                return Result<bool>.Failure(Error.NotFound);
            }

            var tradingSummary = new TradingSummary
            {
                Id = 0,
                TradeId = request.TradeId,
                ExecutiveSummary = result.ExecutiveSummary,
                TechnicalInsights = result.TechnicalInsights,
                PsychologyAnalysis = result.PsychologyAnalysis,
                CriticalMistakes = new CriticalMistakes
                {
                    Technical = result.CriticalMistakes.Technical,
                    Psychological = result.CriticalMistakes.Psychological
                }
            };

            // Store to DB
            await context.TradingSummaries.AddAsync(tradingSummary, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            TradeHistory? tradeHistory = await context.TradeHistories.FindAsync([request.TradeId], cancellationToken);

            if (tradeHistory is not null)
            {
                tradeHistory.TradingSummaryId = tradingSummary.Id;
                context.TradeHistories.Update(tradeHistory);
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result<bool>.Success(true);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trade-histories");

            group.MapPost("/summarize/{tradeId:int}", async (int tradeId, ISender sender) => {
                Result<bool> result = await sender.Send(new SummerizeTradeHistory.Request(tradeId));

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Summarize trade history.")
            .WithDescription("Uses AI to generate a summary for the specified trade history.")
            .WithTags(Tags.TradeHistory);
        }
    }
}
