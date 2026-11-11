using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Auth;
using CrmSuite.Modules.Identity.Application.Auth.Commands;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Identity.Infrastructure.Security;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Identity;

public sealed class LoginCommandHandlerTests
{
    private static readonly DateTime Now = new(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_for_fifteen_minutes()
    {
        var hasher = new BCryptPasswordHasher();
        var user = CreateUser(hasher);
        var handler = CreateHandler(user, hasher, Mock.Of<IJwtTokenService>());

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failure = await handler.Handle(
                new LoginCommand("admin", "wrong-password"),
                CancellationToken.None);

            failure.IsFailure.Should().BeTrue();
        }

        user.FailedLoginCount.Should().Be(5);
        user.LockoutEnd.Should().Be(Now.AddMinutes(15));
        user.IsLockedOut(Now).Should().BeTrue();
    }

    [Fact]
    public async Task Correct_password_is_rejected_while_account_is_locked()
    {
        var hasher = new BCryptPasswordHasher();
        var user = CreateUser(hasher);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RegisterFailedLogin(Now, 5, TimeSpan.FromMinutes(15));
        }

        var handler = CreateHandler(user, hasher, Mock.Of<IJwtTokenService>());
        var result = await handler.Handle(
            new LoginCommand("admin", "Admin@123"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("قفل");
    }

    [Fact]
    public async Task Successful_login_resets_failure_count_and_returns_tokens()
    {
        var hasher = new BCryptPasswordHasher();
        var user = CreateUser(hasher);
        user.RegisterFailedLogin(Now);

        var tokens = new TokenPair(
            new AccessTokenResult("access-token", Now.AddHours(1)),
            new RefreshTokenResult("refresh-token", Now.AddDays(30)));

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(service => service.CreateTokens(
                It.IsAny<User>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(tokens);

        var handler = CreateHandler(user, hasher, jwt.Object);
        var result = await handler.Handle(
            new LoginCommand("admin", "Admin@123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        user.FailedLoginCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    private static LoginCommandHandler CreateHandler(
        User user,
        IPasswordHasher hasher,
        IJwtTokenService jwt)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByUsernameWithAccessAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var permissions = new Mock<IPermissionProvider>();
        permissions.Setup(provider => provider.GetPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var unitOfWork = new Mock<IUnitOfWork>();
        var audit = new Mock<IAuditService>();
        audit.Setup(service => service.RecordAsync(
                It.IsAny<AuditEntry>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(service => service.IpAddress).Returns("127.0.0.1");

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var mapper = new IdentityDtoMapper(new JalaliDateService());

        return new LoginCommandHandler(
            users.Object,
            refreshTokens.Object,
            hasher,
            jwt,
            permissions.Object,
            unitOfWork.Object,
            audit.Object,
            currentUser.Object,
            clock.Object,
            mapper);
    }

    private static User CreateUser(IPasswordHasher hasher) =>
        new(
            "admin",
            hasher.Hash("Admin@123"),
            "مدیر تست",
            "09120000001",
            "admin@test.local");
}
