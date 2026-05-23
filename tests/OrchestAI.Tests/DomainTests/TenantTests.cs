using OrchestAI.Domain.Entities;
using FluentAssertions;

namespace OrchestAI.Tests.DomainTests;

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
}
