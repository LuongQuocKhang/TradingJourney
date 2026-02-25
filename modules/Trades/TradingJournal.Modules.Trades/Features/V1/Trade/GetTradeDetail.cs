namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public class GetTradeDetail
{
    public record Request(int Id) : IQuery<Result<TradeHistoryDetailViewModel>>;

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

    public class Handler(ITradeDbContext tradeDbContext) : IQueryHandler<Request, Result<TradeHistoryDetailViewModel>>
    {
        public async Task<Result<TradeHistoryDetailViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            Domain.TradeHistory? trade = await tradeDbContext.TradeHistories.FindAsync([request.Id], cancellationToken: cancellationToken);

            TradeHistoryDetailViewModel TradeHistoryDetailViewModel = trade.Adapt<TradeHistoryDetailViewModel>();

            if (trade == null)
            {
                return Result<TradeHistoryDetailViewModel>.Failure(Error.NotFound);
            }

            return Result<TradeHistoryDetailViewModel>.Success(TradeHistoryDetailViewModel);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trades");

            group.MapGet("/{id}", async ([FromRoute] int id, ISender sender) => {
                Result<TradeHistoryDetailViewModel> result = await sender.Send(new Request(id));

                return result.IsSuccess ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .Produces<Result<TradeHistoryDetailViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a trade history by ID.")
            .WithDescription("Retrieves a trade history by its ID.") 
            .WithTags(Tags.Trades);
        }
    }
}