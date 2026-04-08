using FluentAssertions;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Dashboard;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Tests.Psychology.Features.V1.Dashboard;

[TestFixture]
public class GetEmotionFrequencyHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private Mock<ICacheRepository> _cacheMock = null!;
    private GetEmotionFrequency.Handler _handler = null!;
    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _cacheMock = new Mock<ICacheRepository>();
        _handler = new GetEmotionFrequency.Handler(_contextMock.Object, _cacheMock.Object);
    }
    [Test]
    public async Task Handle_Returns_Success()
    {
        _contextMock.Setup(x => x.PsychologyJournals).Returns(Array.Empty<PsychologyJournal>().AsQueryable());
        _cacheMock.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<PsychologyStatisticViewModel>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync((PsychologyStatisticViewModel?)null);
        var result = await _handler.Handle(new GetEmotionFrequency.Request(1), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }
}
