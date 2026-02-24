using System.Net;
using Mapster;
using MediatR;
using TradingJournal.Modules.Trades.Common.Constants;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Modules.Trades.Infrastructure;
using TradingJournal.Modules.Trades.ViewModel;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public class GetTradeDetail
{
    public class Request : IQuery<Result<TradeHistoryViewModel>>
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

    public class Handler(ITradeDbContext tradeDbContext) : IQueryHandler<Request, Result<TradeHistoryViewModel>>
    {
        public async Task<Result<TradeHistoryViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            Domain.TradeHistory? trade = await tradeDbContext.TradeHistories.FindAsync([request.Id], cancellationToken: cancellationToken);

            TradeHistoryViewModel tradeHistoryViewModel = trade.Adapt<TradeHistoryViewModel>();

            if (trade == null)
            {
                return Result<TradeHistoryViewModel>.NotFound();
            }

            return Result<TradeHistoryViewModel>.Success(tradeHistoryViewModel);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trades");

            group.MapGet("/{id}", async ([FromRoute] int id, ISender sender) => {
                Result<TradeHistoryViewModel> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .Produces<Result<TradeHistoryViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a trade history by ID.")
            .WithDescription("Retrieves a trade history by its ID.") 
            .WithTags(Tags.Trades);
        }
    }
}