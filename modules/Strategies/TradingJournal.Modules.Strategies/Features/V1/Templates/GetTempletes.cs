using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Templates;

public class GetTemplates
{
    internal class Request : IQuery<Result<List<TemplateViewModel>>>
    {
    }

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<List<TemplateViewModel>>>
    {
        public async Task<Result<List<TemplateViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            List<StrategyTemplate> templates = await context.StrategyTemplates
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync(cancellationToken);

            List<TemplateViewModel> viewModels = templates.Select(t => new TemplateViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category,
                Asset = t.Asset,
                Timeframe = t.Timeframe,
                EntryIndicators = DeserializeIndicators(t.EntryIndicators),
                RiskPerTrade = t.RiskPerTrade,
                CreatedDate = t.CreatedDate
            }).ToList();

            return Result<List<TemplateViewModel>>.Success(viewModels);
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
            RouteGroupBuilder group = app.MapGroup("api/v1/strategy-templates");

            group.MapGet("/", async (ISender sender) =>
            {
                Result<List<TemplateViewModel>> result = await sender.Send(new Request());

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<List<TemplateViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all strategy templates.")
            .WithDescription("Retrieves all strategy templates.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
