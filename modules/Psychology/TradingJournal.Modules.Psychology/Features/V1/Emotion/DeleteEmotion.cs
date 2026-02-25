using TradingJournal.Modules.Psychology.Constants;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;

namespace TradingJournal.Modules.Psychology.Features.V1.Emotion;

public sealed class DeleteEmotion
{
    public sealed class Request : ICommand<Result<bool>>
    {
        public int Id { get; set; }
    }

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator() 
        { 
            RuleFor(x => x.Id)
                .NotEmpty()
                .GreaterThan(0)
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .WithMessage("Id is required");
        }
    }

    internal sealed class Handler(IPsychologyDbContext context) : ICommandHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            EmotionTag? emotionTag = await context.EmotionTags.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            if (emotionTag is null)
            {
                return Result<bool>.Failure(Error.Create("Emotion tag not found."));
            }

            context.EmotionTags.Remove(emotionTag);

            await context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/v1/emotions/{id}", async (int id, IMediator mediator) =>
            {
                Result<bool> result = await mediator.Send(new Request { Id = id });
                return result;
            })
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete an emotion tag.")
            .WithDescription("Deletes an emotion tag/")
            .WithTags(Tags.Emotions);
        }
    }
}
