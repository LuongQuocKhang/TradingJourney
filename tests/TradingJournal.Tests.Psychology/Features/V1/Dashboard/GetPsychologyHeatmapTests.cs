using FluentAssertions;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Dashboard;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Tests.Psychology.Features.V1.Dashboard;

[TestFixture]
public class GetPsychologyHeatmapHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private Mock<ICacheRepository> _cacheMock = null!;
    private GetPsychologyHeatmap.Handler _handler = null!;
    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _cacheMock = new Mock<ICacheRepository>();
        _handler = new GetPsychologyHeatmap.Handler(_contextMock.Object, _cacheMock.Object);
    }
    [Test]
    public async Task Handle_Returns_Empty_Heatmap_When_No_Data()
    {
        _contextMock.Setup(x => x.PsychologyJournals).Returns(Array.Empty<PsychologyJournal>().AsQueryable());
        _cacheMock.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<Result<List<HeatmapDataResponse>>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync((Result<List<HeatmapDataResponse>>?)null);
        var request = new GetPsychologyHeatmap.Request(1);
        var result = await _handler.Handle(request, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }
}
