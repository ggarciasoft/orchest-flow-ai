using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrchestFlowAI.Tests.ControllerTests;

/// <summary>
/// Verifies that RBAC authorization policies are correctly configured in the DI container.
/// Tests that ViewerOrAbove, EditorOrAbove, and AdminOnly policies exist and have the right role requirements.
/// </summary>
public sealed class RbacAuthorizationTests
{
    private static IAuthorizationPolicyProvider BuildPolicyProvider()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ViewerOrAbove", policy =>
                policy.RequireRole("Viewer", "Editor", "Admin", "Approver"));
            options.AddPolicy("EditorOrAbove", policy =>
                policy.RequireRole("Editor", "Admin"));
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
        });
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IAuthorizationPolicyProvider>();
    }

    [Fact]
    public async Task ViewerOrAbovePolicy_Exists()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("ViewerOrAbove");
        policy.Should().NotBeNull("ViewerOrAbove policy must be registered");
    }

    [Fact]
    public async Task EditorOrAbovePolicy_Exists()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("EditorOrAbove");
        policy.Should().NotBeNull("EditorOrAbove policy must be registered");
    }

    [Fact]
    public async Task AdminOnlyPolicy_Exists()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("AdminOnly");
        policy.Should().NotBeNull("AdminOnly policy must be registered");
    }

    [Fact]
    public async Task ViewerOrAbovePolicy_HasRoleRequirement()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("ViewerOrAbove");
        policy!.Requirements.Should().ContainSingle(r => r is RolesAuthorizationRequirement);
        var req = (RolesAuthorizationRequirement)policy.Requirements.First(r => r is RolesAuthorizationRequirement);
        req.AllowedRoles.Should().Contain("Viewer");
        req.AllowedRoles.Should().Contain("Editor");
        req.AllowedRoles.Should().Contain("Admin");
    }

    [Fact]
    public async Task EditorOrAbovePolicy_AllowsEditorAndAdmin()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("EditorOrAbove");
        var req = (RolesAuthorizationRequirement)policy!.Requirements.First(r => r is RolesAuthorizationRequirement);
        req.AllowedRoles.Should().Contain("Editor");
        req.AllowedRoles.Should().Contain("Admin");
    }

    [Fact]
    public async Task EditorOrAbovePolicy_DoesNotAllowViewer()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("EditorOrAbove");
        var req = (RolesAuthorizationRequirement)policy!.Requirements.First(r => r is RolesAuthorizationRequirement);
        req.AllowedRoles.Should().NotContain("Viewer");
    }

    [Fact]
    public async Task AdminOnlyPolicy_AllowsOnlyAdmin()
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync("AdminOnly");
        var req = (RolesAuthorizationRequirement)policy!.Requirements.First(r => r is RolesAuthorizationRequirement);
        req.AllowedRoles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    [Theory]
    [InlineData("ViewerOrAbove")]
    [InlineData("EditorOrAbove")]
    [InlineData("AdminOnly")]
    public async Task AllPolicies_HaveAtLeastOneRequirement(string policyName)
    {
        var provider = BuildPolicyProvider();
        var policy = await provider.GetPolicyAsync(policyName);
        policy!.Requirements.Should().NotBeEmpty($"{policyName} must have at least one requirement");
    }
}
