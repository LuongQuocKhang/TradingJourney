using TradingJournal.Modules.Psychology.Constants;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Modules.Psychology.Features.V1.Emotion;

public sealed class GetEmotions
{
    public sealed class Request : IQuery<Result<List<EmotionTag>>>
    {
    }

    internal sealed class Handler(IPsychologyDbContext context, ICacheRepository cacheRepository) : IQueryHandler<Request, Result<List<EmotionTag>>>
    {
        public async Task<Result<List<EmotionTag>>> Handle(Request request, CancellationToken cancellationToken)
        {
            string key = "emotions";

            return await cacheRepository.GetOrCreateAsync(key, async (cancellationToken) =>
            {
                List<EmotionTag> emotions = await context.EmotionTags
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .ToListAsync(cancellationToken);
                return Result<List<EmotionTag>>.Success(emotions);
            }, TimeSpan.FromMinutes(5), cancellationToken) ?? Result<List<EmotionTag>>.Failure(Error.Create("Failed to get emotions.")); 
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/v1/emotions", async (IMediator mediator) =>
            {
                Result<List<EmotionTag>> result = await mediator.Send(new Request());
                return result;
            })
            .Produces<Result<List<EmotionTag>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all emotion tags.")
            .WithDescription("Gets all emotion tags/")
            .WithTags(Tags.Emotions);
        }
    }
}
