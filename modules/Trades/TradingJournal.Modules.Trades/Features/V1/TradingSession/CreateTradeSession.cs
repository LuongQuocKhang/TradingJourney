using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.TradingSession;

public sealed class CreateTradeSession
{
    public record Request(DateTime FromTime) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FromTime)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime cannot be null.");
        }
    }
    
    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            var tradeSession = request.Adapt<Domain.TradingSession>();

            await context.TradingSessions.AddAsync(tradeSession, cancellationToken);

            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0 ? Result<int>.Success(tradeSession.Id)
                : Result<int>.Failure(Error.Create("Failed to create trade session."));
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trade-sessions");

            group.MapPost("/", async (Request request, ISender sender) =>
            {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess ? Results.Created($"/trade-sessions/{result.Value}", result.Value)
                    : Results.BadRequest(result.Errors);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new trade session.")
            .WithDescription("Creates a new trade session with the given details.")
            .WithTags(Tags.TradingSessions);
        }
    }
}