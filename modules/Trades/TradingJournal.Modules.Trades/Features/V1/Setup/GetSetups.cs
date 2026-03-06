using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.Setup;

public sealed class GetSetups
{
    internal sealed record Request : IQuery<Result<IReadOnlyCollection<SetupViewModel>>>;

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<IReadOnlyCollection<SetupViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<SetupViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<TradingSetup> setups = await context.TradingSetups
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync(cancellationToken);

            IReadOnlyCollection<SetupViewModel> viewModels = setups.Adapt<IReadOnlyCollection<SetupViewModel>>();

            return Result<IReadOnlyCollection<SetupViewModel>>.Success(viewModels);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/setups");

            group.MapGet("/", async (ISender sender) =>
            {
                Result<IReadOnlyCollection<SetupViewModel>> result = await sender.Send(new Request());

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<IReadOnlyCollection<SetupViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all trading setups.")
            .WithDescription("Retrieves all trading setups.")
            .WithTags(Tags.TradingSetups);
        }
    }
}
