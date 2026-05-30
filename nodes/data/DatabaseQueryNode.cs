using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Exceptions;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Data;

/// <summary>
/// Executes a parameterized SELECT query against a relational database and returns the result rows as JSON.
/// Supports PostgreSQL, SQL Server, and MySQL.
/// </summary>
public sealed class DatabaseQueryNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "data.db-query";

    /// <inheritdoc />
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var provider = ctx.GetConfig<string>("provider") ?? "postgresql";
        var connectionString = ctx.GetConfig<string>("connectionString")
            ?? ctx.GetInput<string>("connectionString")
            ?? throw new NodeExecutionException("DB_MISSING_CONNECTION", "connectionString config or input is required.", retryable: false);
        var query = ctx.GetConfig<string>("query")
            ?? throw new NodeExecutionException("DB_MISSING_QUERY", "query config is required.", retryable: false);
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
        command.CommandText = query;
        command.CommandTimeout = timeoutSeconds;

        // Bind named parameters from JSON object, e.g. {"userId": "abc", "status": "active"}
        if (!string.IsNullOrWhiteSpace(parametersJson))
            BindParameters(command, parametersJson, BuildLookup(ctx));

        try
        {
            using var reader = await command.ExecuteReaderAsync(ct);
            var rows = await ReadRowsAsync(reader, ct);
            var rowsJson = JsonSerializer.Serialize(rows);

            return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
            {
                ["rows"] = rowsJson,
                ["rowCount"] = rows.Count,
            });
        }
        catch (NodeExecutionException) { throw; }
        catch (Exception ex)
        {
            throw new NodeExecutionException("DB_QUERY_FAILED", $"Query failed: {ex.Message}", retryable: false);
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

    /// <summary>
    /// Builds a flat lookup of all values available to this node:
    /// workflow inputs, directly-wired NodeInputs, and all upstream NodeOutputs.
    /// This ensures {{key}} templates resolve regardless of direct edge wiring.
    /// </summary>
    private static Dictionary<string, object?> BuildLookup(WorkflowExecutionContext ctx)
    {
        var lookup = new Dictionary<string, object?>(ctx.WorkflowInputs);
        foreach (var kv in ctx.NodeInputs) lookup[kv.Key] = kv.Value;
        foreach (var nodeOutputs in ctx.NodeOutputs.Values)
            foreach (var kv in nodeOutputs)
                lookup.TryAdd(kv.Key, kv.Value);
        return lookup;
    }

    private static void BindParameters(DbCommand command, string parametersJson, IReadOnlyDictionary<string, object?> inputs)
    {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parametersJson)
            ?? new Dictionary<string, JsonElement>();
        foreach (var (key, value) in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = key.TrimStart('@', '$', ':');
            // Resolve {{inputKey}} templates against upstream node outputs / workflow inputs
            object resolvedValue = value.ValueKind switch
            {
                JsonValueKind.Null => DBNull.Value,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when value.TryGetInt64(out var l) => l,
                JsonValueKind.Number => value.GetDouble(),
                _ => (object)ResolveTemplate(value.GetString()!, inputs)
            };
            param.Value = resolvedValue;
            command.Parameters.Add(param);
        }
    }

    /// <summary>Resolves <c>{{key}}</c> placeholders in a string against the provided input dictionary.</summary>
    private static string ResolveTemplate(string template, IReadOnlyDictionary<string, object?> inputs)
        => global::System.Text.RegularExpressions.Regex.Replace(template, @"\{\{(\w+)\}}", match =>
        {
            var key = match.Groups[1].Value;
            return inputs.TryGetValue(key, out var val) ? val?.ToString() ?? string.Empty : match.Value;
        });

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(DbDataReader reader, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, object?>>();
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
}

/// <summary>Descriptor for <see cref="DatabaseQueryNode"/>.</summary>
public sealed class DatabaseQueryNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "data.db-query";
    /// <inheritdoc />
    public string DisplayName => "Database Query";
    /// <inheritdoc />
    public string Description => "Executes a parameterized SELECT query and returns rows as JSON. Supports PostgreSQL, SQL Server, and MySQL.";
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
        new NodeOutputDefinition("rows", "Rows", "Query result rows as a JSON array of objects.", DataType.String),
        new NodeOutputDefinition("rowCount", "Row Count", "Number of rows returned.", DataType.Number),
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("provider", "Provider", "Database provider.", DataType.Enum, Required: true, DefaultValue: "postgresql", AllowedValues: new[] { "postgresql", "sqlserver", "mysql" }),
        new NodeConfigDefinition("connectionString", "Connection String", "Database connection string. Can also be passed as a runtime input.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("query", "SQL Query", "Parameterized SELECT query. Use @paramName placeholders.", DataType.String, Required: true, IsMultiline: true),
        new NodeConfigDefinition("parameters", "Parameters", "JSON object of query parameters, e.g. {\"userId\": \"abc\"}.", DataType.String, Required: false, IsMultiline: true),
        new NodeConfigDefinition("timeoutSeconds", "Timeout (s)", "Query timeout in seconds.", DataType.Number, Required: false, DefaultValue: 30),
    };
}


