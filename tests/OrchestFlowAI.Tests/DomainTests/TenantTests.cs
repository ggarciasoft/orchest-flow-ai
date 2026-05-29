using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.ValueObjects;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class TenantTests
{
    [Fact]
    public void Create_ShouldReturnTenantWithExpectedName()
    {
        var tenant = Tenant.Create("Acme Corp");

        tenant.Id.Should().NotBeEmpty();
        tenant.Name.Should().Be("Acme Corp");
        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TwoTenants_ShouldHaveDifferentIds()
    {
        var t1 = Tenant.Create("T1");
        var t2 = Tenant.Create("T2");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void GetConfig_NewTenant_ReturnsDefaults()
    {
        var tenant = Tenant.Create("Test");

        var config = tenant.GetConfig();

        config.MaxConcurrentExecutions.Should().Be(10);
        config.ExecutionTimeoutSeconds.Should().Be(3600);
        config.DefaultTimezone.Should().Be("UTC");
        config.AllowGuestFormFill.Should().BeTrue();
        config.DisplayName.Should().BeNull();
        config.LogoUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateConfig_PersistsAndRestoresValues()
    {
        var tenant = Tenant.Create("Test");
        var config = new TenantConfig
        {
            DisplayName = "Acme Corp",
            LogoUrl = "https://example.com/logo.png",
            MaxConcurrentExecutions = 5,
            ExecutionTimeoutSeconds = 1800,
            DefaultTimezone = "America/New_York",
            AllowGuestFormFill = false
        };

        tenant.UpdateConfig(config);
        var restored = tenant.GetConfig();

        restored.DisplayName.Should().Be("Acme Corp");
        restored.LogoUrl.Should().Be("https://example.com/logo.png");
        restored.MaxConcurrentExecutions.Should().Be(5);
        restored.ExecutionTimeoutSeconds.Should().Be(1800);
        restored.DefaultTimezone.Should().Be("America/New_York");
        restored.AllowGuestFormFill.Should().BeFalse();
    }

    [Fact]
    public void GetConfig_MalformedJson_ReturnsDefaults()
    {
        var tenant = Tenant.Create("Test");
        // Manually corrupt ConfigJson via reflection to simulate DB corruption
        typeof(Tenant)
            .GetProperty("ConfigJson",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            !.SetValue(tenant, "not-valid-json");

        var config = tenant.GetConfig();

        config.MaxConcurrentExecutions.Should().Be(10); // default
    }
}
