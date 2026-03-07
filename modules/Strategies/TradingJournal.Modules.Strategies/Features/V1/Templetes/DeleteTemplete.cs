namespace TradingJournal.Modules.Strategies.Features.V1.Templetes;

public class DeleteTemplete
{
    public class Request : ICommand<Result<int>>
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

    public class Handler(IStrategyDbContext context) : ICommandHandler<Request, Result<int>>
    {
        public async Task<Result<int>> Handle(Request request, CancellationToken cancellationToken)
        {
            StrategyTemplate? template = await context.StrategyTemplates
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (template == null)
            {
                return Result<int>.Failure(Error.NotFound);
            }

            context.StrategyTemplates.Remove(template);
            await context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(template.Id);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/strategy-templates");

            group.MapDelete("/{id}", async ([FromRoute] int id, ISender sender) =>
            {
                Result<int> result = await sender.Send(new Request { Id = id });

                return result.IsSuccess
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .Produces<Result<int>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete a strategy template by ID.")
            .WithDescription("Deletes a strategy template by its ID.")
            .WithTags(Tags.StrategyTemplate);
        }
    }
}
