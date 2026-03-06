namespace TradingJournal.Modules.Trades.Features.V1.Setup;

public sealed class UpdateSetup
{
    public record StepDto(
        int StepNumber,
        string Label,
        string? Description,
        string NodeType,
        string? Color,
        double PositionX,
        double PositionY);

    public record ConnectionDto(
        int SourceStepIndex,
        int TargetStepIndex,
        string? Label,
        bool IsAnimated,
        string? Color);

    public record Request(
        int Id,
        string Name,
        string Model,
        string? Description,
        SetupStatus Status,
        string? Notes,
        List<StepDto>? Steps,
        List<ConnectionDto>? Connections) : ICommand<Result<bool>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Setup Id must be greater than 0.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Setup name is required.");

            RuleFor(x => x.Model)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Model is required.");

            RuleFor(x => x.Status)
                .Cascade(CascadeMode.Stop)
                .Must(status => Enum.IsDefined(status))
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Status must be a valid SetupStatus value.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                await context.BeginTransaction();

                TradingSetup? setup = await context.TradingSetups
                    .Include(s => s.Steps)
                    .Include(s => s.Connections)
                    .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

                if (setup is null)
                {
                    return Result<bool>.Failure(Error.NotFound);
                }

                // Update scalar fields
                setup.Name = request.Name;
                setup.Model = request.Model;
                setup.Description = request.Description;
                setup.Status = request.Status;
                setup.Notes = request.Notes;

                // Remove existing steps and connections
                context.SetupConnections.RemoveRange(setup.Connections);
                context.SetupSteps.RemoveRange(setup.Steps);
                await context.SaveChangesAsync(cancellationToken);

                // Re-insert steps
                List<SetupStep> insertedSteps = [];
                if (request.Steps is { Count: > 0 })
                {
                    foreach (StepDto stepDto in request.Steps)
                    {
                        SetupStep step = new()
                        {
                            Id = 0,
                            TradingSetupId = setup.Id,
                            StepNumber = stepDto.StepNumber,
                            Label = stepDto.Label,
                            Description = stepDto.Description,
                            NodeType = stepDto.NodeType,
                            Color = stepDto.Color,
                            PositionX = stepDto.PositionX,
                            PositionY = stepDto.PositionY
                        };
                        insertedSteps.Add(step);
                    }
                    await context.SetupSteps.AddRangeAsync(insertedSteps, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                // Re-insert connections
                if (request.Connections is { Count: > 0 } && insertedSteps.Count > 0)
                {
                    List<SetupConnection> connections = [];
                    foreach (ConnectionDto connDto in request.Connections)
                    {
                        if (connDto.SourceStepIndex < 0 || connDto.SourceStepIndex >= insertedSteps.Count ||
                            connDto.TargetStepIndex < 0 || connDto.TargetStepIndex >= insertedSteps.Count)
                            continue;

                        connections.Add(new SetupConnection
                        {
                            Id = 0,
                            TradingSetupId = setup.Id,
                            SourceStepId = insertedSteps[connDto.SourceStepIndex].Id,
                            TargetStepId = insertedSteps[connDto.TargetStepIndex].Id,
                            Label = connDto.Label,
                            IsAnimated = connDto.IsAnimated,
                            Color = connDto.Color
                        });
                    }
                    await context.SetupConnections.AddRangeAsync(connections, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                await context.CommitTransaction();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await context.RollbackTransaction();
                return Result<bool>.Failure(Error.Create(ex.Message));
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/setups");

            group.MapPut("/", async ([FromBody] Request request, ISender sender) =>
            {
                Result<bool> result = await sender.Send(request);

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Update an existing trading setup.")
            .WithDescription("Updates an existing trading setup with the given details.")
            .WithTags(Tags.TradingSetups);
        }
    }
}
