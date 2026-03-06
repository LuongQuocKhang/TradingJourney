using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.Setup;

public sealed class CreateSetup
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
        string Name,
        string Model,
        string? Description,
        SetupStatus Status,
        string? Notes,
        List<StepDto>? Steps,
        List<ConnectionDto>? Connections) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Setup name cannot be null.")
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Setup name cannot be empty.");

            RuleFor(x => x.Model)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Model cannot be null.")
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Model cannot be empty.");

            RuleFor(x => x.Status)
                .Cascade(CascadeMode.Stop)
                .Must(status => Enum.IsDefined(status))
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Status must be a valid SetupStatus value.");
        }
    }

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            try
            {
                await context.BeginTransaction();

                TradingSetup setup = new()
                {
                    Id = 0,
                    Name = request.Name,
                    Model = request.Model,
                    Description = request.Description,
                    Status = request.Status,
                    Notes = request.Notes
                };

                await context.TradingSetups.AddAsync(setup, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                // Insert steps and keep reference for connection mapping
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

                // Insert connections using step index references
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

                return Result<int>.Success(setup.Id);
            }
            catch (Exception ex)
            {
                await context.RollbackTransaction();
                return Result<int>.Failure(Error.Create(ex.Message));
            }
        }
    }

    public sealed class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/setups");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) =>
            {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess
                    ? Results.Created($"/api/v1/setups/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new trading setup.")
            .WithDescription("Creates a new trading setup with optional steps and connections.")
            .WithTags(Tags.TradingSetups);
        }
    }
}
