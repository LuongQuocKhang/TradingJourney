using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.TradingZone;

public sealed class GetTradingZones
{
    internal sealed record Request(int UserId = 0) : IQuery<Result<IReadOnlyCollection<TradingZoneViewModel>>>;

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<IReadOnlyCollection<TradingZoneViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<TradingZoneViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            var tradingZones = await context.TradingZones
                .Where(x => x.CreatedBy == request.UserId)
                .ToListAsync(cancellationToken);

            IReadOnlyCollection<TradingZoneViewModel> tradingZoneViewModels = tradingZones.Adapt<IReadOnlyCollection<TradingZoneViewModel>>();

            return Result<IReadOnlyCollection<TradingZoneViewModel>>.Success(tradingZoneViewModels);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup(ApiGroup.V1.TradingZones);

            group.MapGet("/", async ([FromQuery] int userId, ISender sender) =>
            {
                Result<IReadOnlyCollection<TradingZoneViewModel>> result = await sender.Send(new Request(userId));

                return result.IsSuccess ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<IReadOnlyCollection<TradingZoneViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all trading zones.")
            .WithDescription("Retrieves all trading zones.")
            .WithTags(Tags.TradingZones);
        }
    }
}