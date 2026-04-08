using FluentAssertions;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Psychology;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;

namespace TradingJournal.Tests.Psychology.Features.V1.Psychology;

[TestFixture]
public class UpdatePsychologyJournalHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private UpdatePsychologyJournal.Handler _handler = null!;
    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _handler = new UpdatePsychologyJournal.Handler(_contextMock.Object);
    }
    [Test]
    public async Task Handle_Returns_Failure_When_Journal_Not_Found()
    {
        _contextMock.Setup(x => x.PsychologyJournals.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).ReturnsAsync((PsychologyJournal?)null);
        var request = new UpdatePsychologyJournal.Request(99, OverallMood.Neutral, ConfidentLevel.Neutral, "Notes", new List<int>());
        var result = await _handler.Handle(request, CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }
    [Test]
    public async Task Handle_Returns_Success_When_Found()
    {
        var journal = new PsychologyJournal { Id = 1, Date = DateTime.Now, CreatedBy = 1 };
        _contextMock.Setup(x => x.PsychologyJournals.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(journal);
        _contextMock.Setup(x => x.PsychologyJournalEmotions).Returns(new List<PsychologyJournalEmotion>().AsQueryable());
        var request = new UpdatePsychologyJournal.Request(1, OverallMood.Happy, ConfidentLevel.High, "Updated", new List<int> { 1 });
        var result = await _handler.Handle(request, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
