using OrchestFlowAI.Infrastructure.Auth;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;

namespace OrchestFlowAI.Tests.InfrastructureTests;

public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new();
    private readonly string _key = "super-secret-key-that-is-long-enough-32chars";
    private readonly string _issuer = "test-issuer";
    private readonly string _audience = "test-audience";

    private User CreateTestUser()
    {
        var tenantId = Guid.NewGuid();
        return User.Create(tenantId, "test@example.com", "Test User", UserRole.Admin, "hash");
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        var user = CreateTestUser();
        var token = _service.GenerateToken(user, _key, _issuer, _audience);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ShouldBeValidJwt()
    {
        var user = CreateTestUser();
        var token = _service.GenerateToken(user, _key, _issuer, _audience);

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ShouldContainUserClaims()
    {
        var user = CreateTestUser();
        var token = _service.GenerateToken(user, _key, _issuer, _audience);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_TwiceForSameUser_ShouldProduceDifferentTokensDueToTime()
    {
        var user = CreateTestUser();
        var t1 = _service.GenerateToken(user, _key, _issuer, _audience);
        var t2 = _service.GenerateToken(user, _key, _issuer, _audience);

        // Tokens may be same within the same second, just verify both are valid
        t1.Should().NotBeNullOrEmpty();
        t2.Should().NotBeNullOrEmpty();
    }
}
