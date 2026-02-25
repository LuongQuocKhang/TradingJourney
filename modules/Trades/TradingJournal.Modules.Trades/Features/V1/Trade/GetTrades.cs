using TradingJournal.Shared.Common;
using TradingJournal.Shared.Extensions;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Modules.Trades.Features.V1.Trade;

public class GetTrades
{
    public class Request : IQuery<Result<PaginationViewModel<TradeHistoryViewModel>>>
    {
        public string? Asset { get; set; }

        public PositionType? Position { get; set; }

        public TradeStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

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

    public class Handler(ITradeDbContext tradeDbContext, ICacheRepository cacheRepository) : IQueryHandler<Request, Result<PaginationViewModel<TradeHistoryViewModel>>>
    {
        public async Task<Result<PaginationViewModel<TradeHistoryViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            string queryHash = request.ToHashString();

            Result<PaginationViewModel<TradeHistoryViewModel>>? result = await cacheRepository.GetOrCreateAsync<Result<PaginationViewModel<TradeHistoryViewModel>>>(
                queryHash, async cancellationToken =>
                {
                Result<PaginationViewModel<TradeHistoryViewModel>> result = await GetTradesFromDatabase(request, cancellationToken);
                return result;
            }, 
            expiration: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);

            return result ?? Result<PaginationViewModel<TradeHistoryViewModel>>.Failure(Error.NotFound);
        }

        private async Task<Result<PaginationViewModel<TradeHistoryViewModel>>> GetTradesFromDatabase(Request request, CancellationToken cancellationToken)
        {
            IQueryable<TradeHistory> query = tradeDbContext.TradeHistories
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.Asset))
            {
                query = query.Where(th => th.Asset.Contains(request.Asset));
            }

            if (request.Position.HasValue)
            {
                query = query.Where(th => th.Position == request.Position.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(th => th.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(th => th.Date >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(th => th.Date <= request.ToDate.Value);
            }

            int totalItems = await query.CountAsync(cancellationToken);

            List<TradeHistory> tradeHistories = await query
                .OrderByDescending(th => th.Date)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            if (tradeHistories.Count == 0)
            {
                return Result<PaginationViewModel<TradeHistoryViewModel>>.Failure(Error.NotFound);
            }

            IReadOnlyCollection<TradeHistoryViewModel> tradeHistoryViewModels = tradeHistories.Adapt<IReadOnlyCollection<TradeHistoryViewModel>>();

            foreach (TradeHistoryViewModel viewModel in tradeHistoryViewModels)
            {
                //List<string> emotionTags = await tradeDbContext.TradeEmotionTags
                //    .Where(tet => tet.TradeHistoryId == viewModel.Id)
                //    .Select(tet => tet.EmotionTag.Name)
                //    .ToListAsync(cancellationToken);
                //viewModel.EmotionTags = emotionTags;

                // get emotion tags from cache

                viewModel.Position = viewModel.Position switch
                {
                    "Long" => "Long",
                    "Short" => "Short",
                    _ => viewModel.Position
                };

                viewModel.Status = viewModel.Status switch
                {
                    "Open" => "Open",
                    "Closed" => "Closed",
                    _ => viewModel.Status
                };
            }

            PaginationViewModel<TradeHistoryViewModel> result = new()
            {
                TotalItems = totalItems,
                HasMore = (request.Page * request.PageSize) < totalItems,
                Values = tradeHistoryViewModels
            };

            return Result<PaginationViewModel<TradeHistoryViewModel>>.Success(result);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trades");

            group.MapPost("/", async (ISender sender, [FromBody] Request request) =>
            {
                Result<PaginationViewModel<TradeHistoryViewModel>> result = await sender.Send(request);

                return result.IsSuccess ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<PaginationViewModel<TradeHistoryViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Search trade histories.")
            .WithDescription("Retrieves a list of trade histories.")
            .WithTags(Tags.Trades);
        }
    }
}