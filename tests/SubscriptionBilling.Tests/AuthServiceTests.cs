using FluentAssertions;
using Moq;
using SubscriptionBilling.Application.DTOs.Auth;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Application.Services;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Exceptions;
using Xunit;

namespace SubscriptionBilling.Tests;

public class AuthServiceTests
{
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IStripePaymentGateway> _stripeMock;
    private readonly Mock<IAuditLogService> _auditMock;

    public AuthServiceTests()
    {
        _jwtMock = new Mock<IJwtTokenService>();
        _hasherMock = new Mock<IPasswordHasher>();
        _stripeMock = new Mock<IStripePaymentGateway>();
        _auditMock = new Mock<IAuditLogService>();

        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns("fake_jwt_token_123");

        _jwtMock.Setup(j => j.GenerateRefreshToken(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(new RefreshToken { Token = "fake_refresh_token_abc", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) });

        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashed_pwd_xyz");

        _stripeMock.Setup(s => s.CreateCustomerAsync(It.IsAny<User>()))
            .ReturnsAsync("cus_stripe_mock_123");
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsAuthResponseAndCreatesUser()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(RegisterAsync_WithValidRequest_ReturnsAuthResponseAndCreatesUser));
        var service = new AuthService(context, _jwtMock.Object, _hasherMock.Object, _stripeMock.Object, _auditMock.Object);

        var request = new RegisterRequest("newuser@test.com", "Password123!", "Alice", "Smith");

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("fake_jwt_token_123");
        result.RefreshToken.Should().Be("fake_refresh_token_abc");
        result.User.Email.Should().Be("newuser@test.com");
        result.User.Roles.Should().Contain("Customer");

        var userInDb = context.Users.FirstOrDefault(u => u.Email == "newuser@test.com");
        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().Be("hashed_pwd_xyz");
        userInDb.StripeCustomerId.Should().Be("cus_stripe_mock_123");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsBusinessRuleValidationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(RegisterAsync_WithDuplicateEmail_ThrowsBusinessRuleValidationException));
        context.Users.Add(new User { Email = "duplicate@test.com", PasswordHash = "hash", FirstName = "Bob", LastName = "Jones" });
        await context.SaveChangesAsync();

        var service = new AuthService(context, _jwtMock.Object, _hasherMock.Object, _stripeMock.Object, _auditMock.Object);
        var request = new RegisterRequest("duplicate@test.com", "Password123!", "Bob", "Jones");

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(LoginAsync_WithValidCredentials_ReturnsAuthResponse));
        var role = new Role { Id = 1, Name = "Customer" };
        var user = new User { Id = 10, Email = "login@test.com", PasswordHash = "correct_hash", FirstName = "Dan", LastName = "V", IsActive = true };
        context.Roles.Add(role);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = 10, RoleId = 1 });
        await context.SaveChangesAsync();

        _hasherMock.Setup(h => h.VerifyPassword("Secret123!", "correct_hash")).Returns(true);

        var service = new AuthService(context, _jwtMock.Object, _hasherMock.Object, _stripeMock.Object, _auditMock.Object);

        // Act
        var response = await service.LoginAsync(new LoginRequest("login@test.com", "Secret123!"));

        // Assert
        response.Should().NotBeNull();
        response.User.Email.Should().Be("login@test.com");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsBusinessRuleValidationException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(LoginAsync_WithInvalidPassword_ThrowsBusinessRuleValidationException));
        context.Users.Add(new User { Email = "user@test.com", PasswordHash = "correct_hash", FirstName = "Dan", LastName = "V", IsActive = true });
        await context.SaveChangesAsync();

        _hasherMock.Setup(h => h.VerifyPassword("WrongPassword!", "correct_hash")).Returns(false);

        var service = new AuthService(context, _jwtMock.Object, _hasherMock.Object, _stripeMock.Object, _auditMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            service.LoginAsync(new LoginRequest("user@test.com", "WrongPassword!")));
    }
}
