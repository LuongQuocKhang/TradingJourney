using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using TradingJournal.Modules.Psychology.Domain;
using TradingJournal.Modules.Psychology.Features.V1.Emotion;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Common.Enum;
using TradingJournal.Shared.Dtos;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Tests.Psychology.Features.V1.Emotion;

[TestFixture]
public class CreateEmotionValidatorTests
{
    private static readonly CreateEmotion.Validator _validator = new();

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var request = new CreateEmotion.Request { Name = "", EmotionType = EmotionType.Positive };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Should_Not_Have_Error_When_Valid()
    {
        var request = new CreateEmotion.Request { Name = "Happy", EmotionType = EmotionType.Positive };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyErrors();
    }
}

[TestFixture]
public class CreateEmotionHandlerTests
{
    private Mock<IPsychologyDbContext> _contextMock = null!;
    private Mock<IEmotionTagProvider> _providerMock = null!;
    private CreateEmotion.Handler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IPsychologyDbContext>();
        _providerMock = new Mock<IEmotionTagProvider>();
        _handler = new CreateEmotion.Handler(_contextMock.Object, _providerMock.Object);
    }

    [Test]
    public async Task Handle_Returns_Failure_When_Emotion_Name_Already_Exists()
    {
        var existing = new EmotionTag { Id = 1, Name = "Happy" };
        _contextMock.Setup(x => x.EmotionTags).Returns(new[] { existing }.AsQueryable());
        var request = new CreateEmotion.Request { Name = "Happy", EmotionType = EmotionType.Positive };

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_Returns_Success_When_Emotion_Is_New()
    {
        _contextMock.Setup(x => x.EmotionTags).Returns(Array.Empty<EmotionTag>().AsQueryable());
        var request = new CreateEmotion.Request { Name = "NewEmotion", EmotionType = EmotionType.Positive };

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _contextMock.Verify(x => x.EmotionTags.AddAsync(It.Is<EmotionTag>(e => e.Name == "NewEmotion"), It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
