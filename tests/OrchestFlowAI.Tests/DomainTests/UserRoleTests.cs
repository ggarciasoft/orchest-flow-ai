using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using Xunit;

namespace OrchestFlowAI.Tests.DomainTests;

/// <summary>
/// Tests for UserRole enum values and the User.SetRole method.
/// </summary>
public sealed class UserRoleTests
{
    [Theory]
    [InlineData(UserRole.Viewer)]
    [InlineData(UserRole.Editor)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Approver)]
    public void UserRole_AllExpectedValues_Exist(UserRole role)
    {
        // All expected roles must be defined in the enum
        Enum.IsDefined(typeof(UserRole), role).Should().BeTrue();
    }

    [Fact]
    public void UserRole_HasAtLeastViewerEditorAdmin()
    {
        var roles = Enum.GetValues<UserRole>();
        roles.Should().Contain(UserRole.Viewer);
        roles.Should().Contain(UserRole.Editor);
        roles.Should().Contain(UserRole.Admin);
    }

    [Fact]
    public void SetRole_ChangesUserRole_ToEditor()
    {
        var user = User.Create(Guid.NewGuid(), "test@example.com", "Test", UserRole.Viewer, "hash");

        user.SetRole(UserRole.Editor);

        user.Role.Should().Be(UserRole.Editor);
    }

    [Fact]
    public void SetRole_ChangesUserRole_ToAdmin()
    {
        var user = User.Create(Guid.NewGuid(), "test@example.com", "Test", UserRole.Viewer, "hash");

        user.SetRole(UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void SetRole_ChangesUserRole_ToViewer()
    {
        var user = User.Create(Guid.NewGuid(), "admin@example.com", "Admin", UserRole.Admin, "hash");

        user.SetRole(UserRole.Viewer);

        user.Role.Should().Be(UserRole.Viewer);
    }

    [Fact]
    public void SetRole_DoesNotAffectOtherProperties()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "editor@example.com", "Editor User", UserRole.Editor, "hash99");

        user.SetRole(UserRole.Admin);

        // Only Role should change; other properties remain the same
        user.Email.Should().Be("editor@example.com");
        user.DisplayName.Should().Be("Editor User");
        user.TenantId.Should().Be(tenantId);
        user.PasswordHash.Should().Be("hash99");
    }

    [Fact]
    public void SetRole_CalledMultipleTimes_UsesLastValue()
    {
        var user = User.Create(Guid.NewGuid(), "user@example.com", "User", UserRole.Viewer, "hash");

        user.SetRole(UserRole.Editor);
        user.SetRole(UserRole.Admin);
        user.SetRole(UserRole.Viewer);

        user.Role.Should().Be(UserRole.Viewer);
    }
}
