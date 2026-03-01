using TradingJournal.Modules.Psychology.Constants;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Dtos;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Modules.Psychology.Features.V1.Emotion;

public sealed class GetEmotions
{
    public sealed class Request : IQuery<Result<List<EmotionTagCacheDto>>>
    {
    }

    internal sealed class Handler(IPsychologyDbContext context, ICacheRepository cacheRepository) : IQueryHandler<Request, Result<List<EmotionTagCacheDto>>>
    {
        public async Task<Result<List<EmotionTagCacheDto>>> Handle(Request request, CancellationToken cancellationToken)
        {
            var emotions = await cacheRepository.GetOrCreateAsync<List<EmotionTagCacheDto>>(CacheKeys.EmotionTags,
                async (cancellationToken) =>
                {
                    List<EmotionTag> emotionTags = await context.EmotionTags
                        .AsNoTracking()
                        .OrderBy(x => x.Name)
                        .ToListAsync(cancellationToken);
                    return [.. emotionTags.Select(e => new EmotionTagCacheDto { Id = e.Id, Name = e.Name })];
                },
                expiration: TimeSpan.FromMinutes(5),
                cancellationToken: cancellationToken) ?? [];

            return emotions.Count > 0
                ? Result<List<EmotionTagCacheDto>>.Success(emotions)
                : Result<List<EmotionTagCacheDto>>.Failure(Error.NotFound);
        }

    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/v1/emotions");

            group.MapGet("/", async (IMediator mediator) =>
            {
                Result<List<EmotionTagCacheDto>> result = await mediator.Send(new Request());
                return result;
            })
            .Produces<Result<List<EmotionTagCacheDto>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all emotion tags.")
            .WithDescription("Gets all emotion tags")
            .WithTags(Tags.Emotions);
        }
    }
}
