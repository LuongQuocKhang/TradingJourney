using Mapster;

namespace TradingJournal.Modules.Trades.Features.V1.TradingZone;

public sealed class CreateTradingZone
{
    internal record Request(string Name, string FromTime, string ToTime, string? Description) : ICommand<Result<int>>;

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Trading Zone name cannot be null.")
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Trading Zone name cannot be empty.");
            RuleFor(x => x.FromTime)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime cannot be null.")
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime cannot be empty.")
                .Must(BeAValidTime).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("FromTime must be in a valid time format (e.g., HH:mm).");
            RuleFor(x => x.ToTime)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("ToTime cannot be null.")
                .NotEmpty().WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("ToTime cannot be empty.")
                .Must(BeAValidTime).WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("ToTime must be in a valid time format (e.g., HH:mm).");
        }
        private bool BeAValidTime(string time)
        {
            return TimeSpan.TryParse(time, out _);
        }
    }

    internal sealed class Handler(ITradeDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            Domain.TradingZone tradingZone = request.Adapt<Domain.TradingZone>();

            await context.TradingZones.AddAsync(tradingZone, cancellationToken);

            int insertedRow = await context.SaveChangesAsync(cancellationToken);

            return insertedRow > 0 ? Result<int>.Success(tradingZone.Id)
                : Result<int>.Failure(Error.Create("Failed to create Trading Zone."));
        }
    }

    public sealed class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/trading-zones");

            group.MapPost("/", async ([FromBody] Request request, ISender sender) => {
                Result<int> result = await sender.Send(request);

                return result.IsSuccess ? Results.Created($"/api/v1/trading-zones/{result.Value}", result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new trading zone.")
            .WithDescription("Creates a new trading zone with the given details.")
            .WithTags(Tags.TradingZones);
        }
    }
}
