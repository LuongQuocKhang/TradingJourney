using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.Setup;

public sealed class GetSetupDetail
{
    internal sealed record Request(int Id) : IQuery<Result<SetupDetailViewModel>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Setup Id must be greater than 0.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : IQueryHandler<Request, Result<SetupDetailViewModel>>
    {
        public async Task<Result<SetupDetailViewModel>> Handle(Request request, CancellationToken cancellationToken)
        {
            TradingSetup? setup = await context.TradingSetups
                .AsNoTracking()
                .Include(s => s.Steps.OrderBy(step => step.StepNumber))
                .Include(s => s.Connections)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (setup is null)
            {
                return Result<SetupDetailViewModel>.Failure(Error.NotFound);
            }

            SetupDetailViewModel viewModel = new()
            {
                Id = setup.Id,
                Name = setup.Name,
                Model = setup.Model,
                Description = setup.Description,
                Status = setup.Status,
                Notes = setup.Notes,
                CreatedDate = setup.CreatedDate,
                UpdatedDate = setup.UpdatedDate,
                Steps = setup.Steps.Select(step => new SetupStepViewModel
                {
                    Id = step.Id,
                    StepNumber = step.StepNumber,
                    Label = step.Label,
                    Description = step.Description,
                    NodeType = step.NodeType,
                    Color = step.Color,
                    PositionX = step.PositionX,
                    PositionY = step.PositionY
                }).ToList(),
                Connections = setup.Connections.Select(conn => new SetupConnectionViewModel
                {
                    Id = conn.Id,
                    SourceStepId = conn.SourceStepId,
                    TargetStepId = conn.TargetStepId,
                    Label = conn.Label,
                    IsAnimated = conn.IsAnimated,
                    Color = conn.Color
                }).ToList()
            };

            return Result<SetupDetailViewModel>.Success(viewModel);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/setups");

            group.MapGet("/{id:int}", async (int id, ISender sender) =>
            {
                Result<SetupDetailViewModel> result = await sender.Send(new Request(id));

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<SetupDetailViewModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a trading setup by ID.")
            .WithDescription("Retrieves a trading setup with its steps and connections.")
            .WithTags(Tags.TradingSetups);
        }
    }
}
