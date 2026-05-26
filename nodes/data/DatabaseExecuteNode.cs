using System.Data.Common;
using System.Text.Json;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Exceptions;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Data;

/// <summary>
/// Executes a non-query SQL statement (INSERT, UPDATE, DELETE) against a relational database.
/// Returns the number of rows affected.
/// Supports PostgreSQL, SQL Server, and MySQL.
/// </summary>
public sealed class DatabaseExecuteNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "data.db-execute";

    /// <inheritdoc />
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var provider = ctx.GetConfig<string>("provider") ?? "postgresql";
        var connectionString = ctx.GetConfig<string>("connectionString")
            ?? ctx.GetInput<string>("connectionString")
            ?? throw new NodeExecutionException("DB_MISSING_CONNECTION", "connectionString config or input is required.", retryable: false);
        var statement = ctx.GetConfig<string>("statement")
            ?? throw new NodeExecutionException("DB_MISSING_STATEMENT", "statement config is required.", retryable: false);
        var parametersJson = ctx.GetConfig<string>("parameters");
        var timeoutSeconds = ctx.GetConfig<int?>("timeoutSeconds") ?? 30;

        using var connection = CreateConnection(provider, connectionString);
        try
        {
            await connection.OpenAsync(ct);
        }
        catch (Exception ex)
        {
            throw new NodeExecutionException("DB_CONNECTION_FAILED", $"Failed to connect: {ex.Message}", retryable: true);
        }

        using var command = connection.CreateCommand();
        command.CommandText = statement;
        command.CommandTimeout = timeoutSeconds;

        if (!string.IsNullOrWhiteSpace(parametersJson))
            BindParameters(command, parametersJson);

        try
        {
            var rowsAffected = await command.ExecuteNonQueryAsync(ct);
            return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
            {
                ["rowsAffected"] = rowsAffected,
                ["success"] = true,
            });
        }
        catch (NodeExecutionException) { throw; }
        catch (Exception ex)
        {
            throw new NodeExecutionException("DB_EXECUTE_FAILED", $"Statement failed: {ex.Message}", retryable: false);
        }
    }

    private static DbConnection CreateConnection(string provider, string connectionString) =>
        provider.ToLowerInvariant() switch
        {
            "postgresql" or "postgres" => new Npgsql.NpgsqlConnection(connectionString),
            "sqlserver" or "mssql" => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            "mysql" or "mariadb" => new MySqlConnector.MySqlConnection(connectionString),
            _ => throw new NodeExecutionException("DB_UNKNOWN_PROVIDER", $"Unknown provider '{provider}'. Use postgresql, sqlserver, or mysql.", retryable: false)
        };

    private static void BindParameters(DbCommand command, string parametersJson)
    {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parametersJson)
            ?? new Dictionary<string, JsonElement>();
        foreach (var (key, value) in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = key.TrimStart('@', '$', ':');
            param.Value = value.ValueKind switch
            {
                JsonValueKind.Null => DBNull.Value,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when value.TryGetInt64(out var l) => l,
                JsonValueKind.Number => value.GetDouble(),
                _ => (object)value.GetString()!
            };
            command.Parameters.Add(param);
        }
    }
}

/// <summary>Descriptor for <see cref="DatabaseExecuteNode"/>.</summary>
public sealed class DatabaseExecuteNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "data.db-execute";
    /// <inheritdoc />
    public string DisplayName => "Database Execute";
    /// <inheritdoc />
    public string Description => "Executes an INSERT, UPDATE, or DELETE statement and returns the number of rows affected. Supports PostgreSQL, SQL Server, and MySQL.";
    /// <inheritdoc />
    public string Category => "data";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "database";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("connectionString", "Connection String", "Optional — override the config connection string at runtime.", DataType.String, Required: false),
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("rowsAffected", "Rows Affected", "Number of rows affected by the statement.", DataType.Number),
        new NodeOutputDefinition("success", "Success", "True if the statement executed without error.", DataType.Boolean),
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("provider", "Provider", "Database provider.", DataType.Enum, Required: true, DefaultValue: "postgresql", AllowedValues: new[] { "postgresql", "sqlserver", "mysql" }),
        new NodeConfigDefinition("connectionString", "Connection String", "Database connection string. Can also be passed as a runtime input.", DataType.String, Required: false),
        new NodeConfigDefinition("statement", "SQL Statement", "Parameterized INSERT/UPDATE/DELETE statement. Use @paramName placeholders.", DataType.String, Required: true),
        new NodeConfigDefinition("parameters", "Parameters", "JSON object of statement parameters, e.g. {\"id\": \"abc\", \"status\": \"active\"}.", DataType.String, Required: false),
        new NodeConfigDefinition("timeoutSeconds", "Timeout (s)", "Statement timeout in seconds.", DataType.Number, Required: false, DefaultValue: 30),
    };
}
