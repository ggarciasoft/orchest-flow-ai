using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine;

namespace OrchestFlowAI.Infrastructure.Security;

public sealed class SecretService : ISecretService, ISecretResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEncryptionService _encryption;
    private static readonly Regex _pattern = new(@"\{\{secret:([^}]+)\}\}", RegexOptions.Compiled);

    public SecretService(IServiceScopeFactory scopeFactory, IEncryptionService encryption)
    { _scopeFactory = scopeFactory; _encryption = encryption; }

    public async Task<string?> ResolveAsync(string? value, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var matches = _pattern.Matches(value);
        if (matches.Count == 0) return value;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISecretRepository>();

        var result = value;
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value.Trim();
            var secret = await repo.GetByNameAsync(name, tenantId, ct);
            if (secret != null)
            {
                var decrypted = _encryption.Decrypt(secret.EncryptedValue);
                result = result.Replace(match.Value, decrypted);
            }
        }
        return result;
    }

    public async Task<Dictionary<string, object?>> ResolveConfigAsync(Dictionary<string, object?> config, Guid tenantId, CancellationToken ct = default)
    {
        var resolved = new Dictionary<string, object?>();
        foreach (var kv in config)
        {
            if (kv.Value is string strVal)
                resolved[kv.Key] = await ResolveAsync(strVal, tenantId, ct);
            else if (kv.Value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
                resolved[kv.Key] = await ResolveAsync(je.GetString(), tenantId, ct);
            else
                resolved[kv.Key] = kv.Value;
        }
        return resolved;
    }
}
