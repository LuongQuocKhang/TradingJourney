using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.TradeSession;

public sealed class CreateTradeSession
{
    public record Request(string Name, string? Description, string FromTime, string ToTime) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Name cannot be null.");

            RuleFor(x => x.FromTime)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime cannot be null.")
                .Must(BeAValidDateTime).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime must be a valid date time string.");

            RuleFor(x => x.ToTime)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("ToTime cannot be null.")
                .Must(BeAValidDateTime).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("ToTime must be a valid date time string.");

            RuleFor(x => new { x.FromTime, x.ToTime })
                .Must(x =>
                {
                    if (DateTime.TryParse(x.FromTime, out var from) && DateTime.TryParse(x.ToTime, out var to))
                    {
                        return from < to;
                    }
                    return false;
                })
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime must be earlier than ToTime.");
        }

        private bool BeAValidDateTime(string dateTime)
        {
            return DateTime.TryParse(dateTime, out _);
        }
    }
    
    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            var tradeSession = request.Adapt<Domain.TradingZone>();

            var id = await context.TradingZones.AddAsync(tradeSession, cancellationToken);

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
                var result = await sender.Send(request);

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