using System.Net;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Modules.Trades.Common.Constants;
using TradingJournal.Modules.Trades.Common.Enum;
using TradingJournal.Modules.Trades.Domain;
using TradingJournal.Modules.Trades.Infrastructure;
using TradingJournal.Modules.Trades.ViewModel;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public class GetTrades
{
    public class Request : IQuery<Result<IReadOnlyCollection<TradeHistoryViewModel>>>
    {
        public string? Asset { get; set; }

        public PositionType? Position { get; set; }

        public TradeStatus? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Page size must be greater than 0.");
        }
    }

    public class Handler(ITradeDbContext tradeDbContext) : IQueryHandler<Request, Result<IReadOnlyCollection<TradeHistoryViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<TradeHistoryViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<TradeHistory> tradeHistories = await tradeDbContext.TradeHistories
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            if (tradeHistories.Count == 0)
            {
                return Result<IReadOnlyCollection<TradeHistoryViewModel>>.NotFound();
            }

            IReadOnlyCollection<TradeHistoryViewModel> tradeHistoryViewModels = tradeHistories.Adapt<IReadOnlyCollection<TradeHistoryViewModel>>();


            return Result<IReadOnlyCollection<TradeHistoryViewModel>>.Success(tradeHistoryViewModels);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trades");

            group.MapGet("/", async ([FromQuery] int page, [FromQuery] int pageSize, ISender sender) => {
                Result<IReadOnlyCollection<TradeHistoryViewModel>> result = await sender.Send(new Request { Page = page, PageSize = pageSize });

                return result.IsSuccess ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .Produces<Result<IReadOnlyCollection<TradeHistoryViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a list of trade histories.")
            .WithDescription("Retrieves a list of trade histories.") 
            .WithTags(Tags.Trades);
        }
    }
}