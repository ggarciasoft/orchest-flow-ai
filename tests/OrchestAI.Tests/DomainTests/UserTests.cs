using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;
using FluentAssertions;

namespace OrchestAI.Tests.DomainTests;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldReturnUserWithExpectedProperties()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "alice@example.com", "Alice", UserRole.Admin, "hash123");

        user.Id.Should().NotBeEmpty();
        user.TenantId.Should().Be(tenantId);
        user.Email.Should().Be("alice@example.com");
        user.DisplayName.Should().Be("Alice");
        user.Role.Should().Be(UserRole.Admin);
        user.PasswordHash.Should().Be("hash123");
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TwoUsers_ShouldHaveDifferentIds()
    {
        var tenantId = Guid.NewGuid();
        var u1 = User.Create(tenantId, "a@x.com", "A", UserRole.Editor, "h1");
        var u2 = User.Create(tenantId, "b@x.com", "B", UserRole.Viewer, "h2");

        u1.Id.Should().NotBe(u2.Id);
    }
}
