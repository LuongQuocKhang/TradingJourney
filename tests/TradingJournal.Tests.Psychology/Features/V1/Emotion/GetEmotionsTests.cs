using FluentAssertions;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Emotion;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;

namespace TradingJournal.Tests.Psychology.Features.V1.Emotion;

[TestFixture]
public class GetEmotionsHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private GetEmotions.Handler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _handler = new GetEmotions.Handler(_contextMock.Object);
    }

    [Test]
    public async Task Handle_Returns_Emotions_When_Request_Are_Present()
    {
        var emotions = new List<EmotionTag>
        {
            new() { Id = 1, Name = "Happy" },
            new() { Id = 2, Name = "Sad" },
        }.AsQueryable();
        _contextMock.Setup(x => x.EmotionTags).Returns(emotions);
        var request = new GetEmotions.Request(1, "");

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_Filters_By_Name_When_Search_Provided()
    {
        var emotions = new List<EmotionTag>
        {
            new() { Id = 1, Name = "Happy" },
            new() { Id = 2, Name = "Sad" },
        }.AsQueryable();
        _contextMock.Setup(x => x.EmotionTags).Returns(emotions);
        var request = new GetEmotions.Request(1, "Happy");

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Happy");
    }
}
