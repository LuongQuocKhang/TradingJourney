namespace TradingJournal.Modules.Trades.Features.V1.ChecklistModels;

public sealed class GetChecklistModels
{
    internal record Request() : ICommand<Result<IReadOnlyCollection<ChecklistModelViewModel>>>;

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<IReadOnlyCollection<ChecklistModelViewModel>>>
    {
        public async Task<Result<IReadOnlyCollection<ChecklistModelViewModel>>> Handle(Request request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ChecklistModelViewModel> models = await context.ChecklistModels
                .AsNoTracking()
                .Select(m => new ChecklistModelViewModel(
                    m.Id,
                    m.Name,
                    m.Description,
                    m.Criteria.Count))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyCollection<ChecklistModelViewModel>>.Success(models);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup(ApiGroup.V1.ChecklistModels);

            group.MapGet("/", async (ISender sender) =>
            {
                Result<IReadOnlyCollection<ChecklistModelViewModel>> result = await sender.Send(new Request());
                return result.IsSuccess ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<IReadOnlyCollection<ChecklistModelViewModel>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Get all checklist models.")
            .WithTags(Tags.ChecklistModels);
        }
    }
}
