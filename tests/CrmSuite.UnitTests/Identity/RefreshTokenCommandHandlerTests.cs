using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Auth.Commands;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Identity;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly DateTime Now = new(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Revoked_refresh_token_cannot_be_used_again()
    {
        var user = new User("admin", "hash", "مدیر", "09120000001", "admin@test.local");
        var refreshToken = new RefreshToken(
            "revoked-token",
            user.Id,
            Now.AddDays(30),
            "127.0.0.1",
            Now.AddDays(-1));
        refreshToken.Revoke(Now, "127.0.0.1");

        var refreshTokens = new Mock<IRefreshTokenRepository>();
        refreshTokens.Setup(repository => repository.GetByTokenAsync(
                "revoked-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var users = new Mock<IUserRepository>();
        var jwt = new Mock<IJwtTokenService>();
        var permissions = new Mock<IPermissionProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var audit = new Mock<IAuditService>();
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(service => service.IpAddress).Returns("127.0.0.1");

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new RefreshTokenCommandHandler(
            refreshTokens.Object,
            users.Object,
            jwt.Object,
            permissions.Object,
            unitOfWork.Object,
            audit.Object,
            currentUser.Object,
            clock.Object,
            new IdentityDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new RefreshTokenCommand("revoked-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("معتبر");
        users.Verify(
            repository => repository.GetByIdWithAccessAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
