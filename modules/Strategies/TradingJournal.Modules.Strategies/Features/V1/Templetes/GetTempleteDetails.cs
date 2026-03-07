using System.Text.Json;

namespace TradingJournal.Modules.Strategies.Features.V1.Templetes;

public class GetTempleteDetails
{
    public class Request : IQuery<Result<TemplateDetailViewModel>>
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
                .WithMessage("Template ID must be greater than 0.");
        }
    }

    internal sealed class Handler(IStrategyDbContext context) : IQueryHandler<Request, Result<TemplateDetailViewModel>>
    {
        public async Task<Result<TemplateDetailViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            StrategyTemplate? template = await context.StrategyTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (template == null)
            {
                return Result<TemplateDetailViewModel>.Failure(Error.NotFound);
            }

            TemplateDetailViewModel viewModel = new()
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Asset = template.Asset,
                Timeframe = template.Timeframe,
                DateRangeStart = template.DateRangeStart,
                DateRangeEnd = template.DateRangeEnd,
                EntryIndicators = DeserializeIndicators(template.EntryIndicators),
                ExitIndicators = DeserializeIndicators(template.ExitIndicators),
                RiskPerTrade = template.RiskPerTrade,
                StopLossType = template.StopLossType,
                StopLossValue = template.StopLossValue,
                TakeProfitType = template.TakeProfitType,
                TakeProfitValue = template.TakeProfitValue,
                PositionSizing = template.PositionSizing,
                PositionSizeValue = template.PositionSizeValue,
                CreatedDate = template.CreatedDate
            };

            return Result<TemplateDetailViewModel>.Success(viewModel);
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

            group.MapGet("/{id}", async ([FromRoute] int id, ISender sender) =>
            {
                Result<TemplateDetailViewModel> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<TemplateDetailViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get strategy template details by ID.")
            .WithDescription("Retrieves a single strategy template with all configuration details.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
