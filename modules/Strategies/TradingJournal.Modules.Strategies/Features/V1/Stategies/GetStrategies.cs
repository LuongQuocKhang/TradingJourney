using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Stategies;

public class GetStrategies
{
    internal class Request : IQuery<Result<List<StrategyViewModel>>>
    {
    }

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<List<StrategyViewModel>>>
    {
        public async Task<Result<List<StrategyViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<Strategy> strategies = await context.Strategies
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync(cancellationToken);

            List<StrategyViewModel> viewModels = strategies.Select(s => new StrategyViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Type = s.Type,
                Status = s.Status,
                Asset = s.Asset,
                Timeframe = s.Timeframe,
                EntryIndicators = DeserializeIndicators(s.EntryIndicators),
                ExitIndicators = DeserializeIndicators(s.ExitIndicators),
                RiskPerTrade = s.RiskPerTrade,
                CreatedDate = s.CreatedDate
            }).ToList();

            return Result<List<StrategyViewModel>>.Success(viewModels);
        }

        private static List<string> DeserializeIndicators(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategies");

            group.MapGet("/", async (ISender sender) =>
            {
                Result<List<StrategyViewModel>> result = await sender.Send(new Request());

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<List<StrategyViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all strategies.")
            .WithDescription("Retrieves all trading strategies.")
            .WithTags(Tags.Strategy);
        }
    }
}
