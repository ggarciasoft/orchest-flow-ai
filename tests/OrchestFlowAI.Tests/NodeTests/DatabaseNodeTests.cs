using System.Text.Json;
using FluentAssertions;
using OrchestFlowAI.Nodes.Data;
using OrchestFlowAI.SDK.Exceptions;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="DatabaseQueryNode"/>.</summary>
public sealed class DatabaseQueryNodeTests
{
    private readonly DatabaseQueryNode _node = new();

    // ---------------------------------------------------------------------------
    // Descriptor tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Descriptor_HasCorrectType()
    {
        var desc = new DatabaseQueryNodeDescriptor();
        desc.Type.Should().Be("data.db-query");
        desc.Category.Should().Be("data");
    }

    [Fact]
    public void Descriptor_HasRequiredOutputs()
    {
        var desc = new DatabaseQueryNodeDescriptor();
        desc.Outputs.Should().Contain(o => o.Key == "rows");
        desc.Outputs.Should().Contain(o => o.Key == "rowCount");
    }

    [Fact]
    public void Descriptor_HasRequiredConfigFields()
    {
        var desc = new DatabaseQueryNodeDescriptor();
        desc.Configuration.Should().Contain(c => c.Key == "provider");
        desc.Configuration.Should().Contain(c => c.Key == "connectionString");
        desc.Configuration.Should().Contain(c => c.Key == "query");
        desc.Configuration.Should().Contain(c => c.Key == "parameters");
        desc.Configuration.Should().Contain(c => c.Key == "timeoutSeconds");
    }

    [Fact]
    public void Descriptor_ProviderAllowedValues_ContainsAllSupportedDatabases()
    {
        var desc = new DatabaseQueryNodeDescriptor();
        var providerField = desc.Configuration.Single(c => c.Key == "provider");
        providerField.AllowedValues.Should().Contain("postgresql");
        providerField.AllowedValues.Should().Contain("sqlserver");
        providerField.AllowedValues.Should().Contain("mysql");
    }

    [Fact]
    public void NodeType_MatchesDescriptorType()
    {
        _node.Type.Should().Be(new DatabaseQueryNodeDescriptor().Type);
    }

    // ---------------------------------------------------------------------------
    // Validation — these throw before any DB connection is attempted
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_MissingConnectionString_ThrowsWithCorrectCode()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "postgresql", ["query"] = "SELECT 1" })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_MISSING_CONNECTION" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_MissingQuery_ThrowsWithCorrectCode()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "postgresql", ["connectionString"] = "Host=localhost" })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_MISSING_QUERY" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProvider_ThrowsWithCorrectCode()
    {
        // Unknown provider throws before any connection attempt
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["provider"] = "oracle",
                ["connectionString"] = "DSN=mydb",
                ["query"] = "SELECT 1"
            })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_UNKNOWN_PROVIDER" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_ConnectionStringFromInput_UsedWhenConfigMissing()
    {
        // Connection string provided as runtime input rather than config
        // Should fail at connection attempt (not at validation) — proves it was accepted
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "oracle", ["query"] = "SELECT 1" })
            .WithInputs(new() { ["connectionString"] = "DSN=mydb" })
            .Build();

        // Should fail with provider error, not missing-connection error
        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_UNKNOWN_PROVIDER");
    }
}

/// <summary>Unit tests for <see cref="DatabaseExecuteNode"/>.</summary>
public sealed class DatabaseExecuteNodeTests
{
    private readonly DatabaseExecuteNode _node = new();

    // ---------------------------------------------------------------------------
    // Descriptor tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Descriptor_HasCorrectType()
    {
        var desc = new DatabaseExecuteNodeDescriptor();
        desc.Type.Should().Be("data.db-execute");
        desc.Category.Should().Be("data");
    }

    [Fact]
    public void Descriptor_HasRequiredOutputs()
    {
        var desc = new DatabaseExecuteNodeDescriptor();
        desc.Outputs.Should().Contain(o => o.Key == "rowsAffected");
        desc.Outputs.Should().Contain(o => o.Key == "success");
    }

    [Fact]
    public void Descriptor_HasRequiredConfigFields()
    {
        var desc = new DatabaseExecuteNodeDescriptor();
        desc.Configuration.Should().Contain(c => c.Key == "provider");
        desc.Configuration.Should().Contain(c => c.Key == "connectionString");
        desc.Configuration.Should().Contain(c => c.Key == "statement");
        desc.Configuration.Should().Contain(c => c.Key == "parameters");
        desc.Configuration.Should().Contain(c => c.Key == "timeoutSeconds");
    }

    [Fact]
    public void NodeType_MatchesDescriptorType()
    {
        _node.Type.Should().Be(new DatabaseExecuteNodeDescriptor().Type);
    }

    // ---------------------------------------------------------------------------
    // Validation — throw before any DB connection is attempted
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_MissingConnectionString_ThrowsWithCorrectCode()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "postgresql", ["statement"] = "DELETE FROM logs" })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_MISSING_CONNECTION" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_MissingStatement_ThrowsWithCorrectCode()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "postgresql", ["connectionString"] = "Host=localhost" })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_MISSING_STATEMENT" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProvider_ThrowsWithCorrectCode()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["provider"] = "oracle",
                ["connectionString"] = "DSN=mydb",
                ["statement"] = "UPDATE foo SET x=1"
            })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_UNKNOWN_PROVIDER" && !e.Retryable);
    }

    [Fact]
    public async Task ExecuteAsync_ConnectionStringFromInput_UsedWhenConfigMissing()
    {
        // Falls through to provider check, not connection-missing check
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["provider"] = "oracle", ["statement"] = "UPDATE foo SET x=1" })
            .WithInputs(new() { ["connectionString"] = "DSN=mydb" })
            .Build();

        var act = () => _node.ExecuteAsync(ctx, CancellationToken.None);

        await act.Should().ThrowAsync<NodeExecutionException>()
            .Where(e => e.Code == "DB_UNKNOWN_PROVIDER");
    }
}
