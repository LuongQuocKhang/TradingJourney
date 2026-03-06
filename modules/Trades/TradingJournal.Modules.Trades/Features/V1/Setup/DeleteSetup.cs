namespace TradingJournal.Modules.Trades.Features.V1.Setup;

public sealed class DeleteSetup
{
    internal sealed record Request(int Id) : ICommand<Result<bool>>;

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

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            TradingSetup? setup = await context.TradingSetups
                .Include(s => s.Steps)
                .Include(s => s.Connections)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (setup is null)
            {
                return Result<bool>.Failure(Error.NotFound);
            }

            context.TradingSetups.Remove(setup);

            await context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/setups");

            group.MapDelete("/{id:int}", async (int id, ISender sender) =>
            {
                Result<bool> result = await sender.Send(new Request(id));

                return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete a trading setup by ID.")
            .WithDescription("Deletes a trading setup and its associated steps and connections.")
            .WithTags(Tags.TradingSetups);
        }
    }
}
