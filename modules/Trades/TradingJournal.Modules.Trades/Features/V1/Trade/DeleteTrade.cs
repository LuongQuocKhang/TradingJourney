namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public class DeleteTrade
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
                .WithMessage("Trade ID must be greater than 0.");
        }
    }

    public class Handler(ITradeDbContext tradeDbContext) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            Domain.TradeHistory? trade = await tradeDbContext.TradeHistories
                .Include(x => x.TradeEmotionTags)
                .Include(x => x.TradeHistorySession)
                .Include(x => x.RiskGuardrail)
                .Include(x => x.TradeScreenShots)
                .Include(x => x.PretradeChecklists)
                .Include(x => x.TechnicalAnalysisTags)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);

            if (trade == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            tradeDbContext.TradeHistories.Remove(trade);

            await tradeDbContext.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(trade.Id);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trade-histories");

            group.MapDelete("/{id}", async ([FromRoute] int id, ISender sender) => {
                Result<int> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete a trade history by ID.")
            .WithDescription("Deletes a trade history by its ID.") 
            .WithTags(Tags.TradeHistory);
        }
    }
}