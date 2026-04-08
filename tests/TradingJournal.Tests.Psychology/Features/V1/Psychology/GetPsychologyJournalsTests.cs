using FluentAssertions;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Psychology;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Tests.Psychology.Features.V1.Psychology;

[TestFixture]
public class GetPsychologyJournalsHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private GetPsychologyJournals.Handler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _handler = new GetPsychologyJournals.Handler(_contextMock.Object);
    }

    [Test]
    public async Task Handle_Returns_Empty_When_No_Journals()
    {
        _contextMock.Setup(x => x.PsychologyJournals).Returns(Array.Empty<PsychologyJournal>().AsQueryable());
        var request = new GetPsychologyJournals.Request(1, DateTime.Now.AddMonths(-1), DateTime.Now, 1);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().Be(0);
    }
}
