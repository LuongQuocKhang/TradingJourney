using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using TradingJournal.Modules.Auth;
using TradingJournal.Modules.Auth.Features.V1.Auth;
using TradingJournal.Modules.Auth.Infrastructure;

namespace TradingJournal.Tests.Auth.Features.V1.Auth;

[TestFixture]
public class LoginValidatorTests
{
    private static readonly Login.Validator _validator = new();

    [Test]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var request = new Login.Request("", "password123");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var request = new Login.Request("test@example.com", "");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Should_Not_Have_Error_When_Both_Are_Filled()
    {
        var request = new Login.Request("test@example.com", "password123");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}

[TestFixture]
public class LoginHandlerTests
{
    private Mock<IAuthDbContext> _contextMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Login.Handler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _contextMock = new Mock<IAuthDbContext>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x["Jwt:Secret"]).Returns("a-very-long-secret-key-that-is-at-least-32-chars!");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("TradingJournal");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("TradingJournal");
        _configMock.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60");
        _configMock.Setup(x => x.GetValue<int>("Jwt:ExpiryMinutes", 60)).Returns(60);
        _handler = new Login.Handler(_contextMock.Object, _configMock.Object);
    }

    [Test]
    public async Task Handle_Returns_Success_When_Credentials_Are_Valid()
    {
        var userSet = new Mock<DbSet<User>>();
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), FullName = "Test User", IsActive = true };
        userSet.Setup(x => x.Include(It.IsAny<string>())).Returns(userSet.Object);
        _contextMock.Setup(x => x.Users).Returns(userSet.Object);
        userSet.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new Login.Request("test@example.com", "password123");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Token.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Handle_Returns_Failure_When_User_Not_Found()
    {
        // Arrange
        var userSet = new Mock<DbSet<User>>();
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), FullName = "Test User", IsActive = true };
        userSet.Setup(x => x.Include(It.IsAny<string>())).Returns(userSet.Object);
        _contextMock.Setup(x => x.Users).Returns(userSet.Object);
        userSet.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var request = new Login.Request("nonexistent@example.com", "password123");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_Returns_Failure_When_Password_Is_Wrong()
    {
        // Arrange
        var userSet = new Mock<DbSet<User>>();
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), FullName = "Test User", IsActive = true };
        userSet.Setup(x => x.Include(It.IsAny<string>())).Returns(userSet.Object);
        _contextMock.Setup(x => x.Users).Returns(userSet.Object);
        userSet.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var request = new Login.Request("test@example.com", "wrongpassword");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_Returns_Failure_When_Account_Is_Disabled()
    {
        // Arrange
        var userSet = new Mock<DbSet<User>>();
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), FullName = "Test User", IsActive = true };
        userSet.Setup(x => x.Include(It.IsAny<string>())).Returns(userSet.Object);
        _contextMock.Setup(x => x.Users).Returns(userSet.Object);
        userSet.Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new Login.Request("test@example.com", "password123");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
