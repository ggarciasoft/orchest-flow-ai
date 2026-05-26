using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.SDK.Interfaces;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/nodes"), Authorize]
public sealed class NodesController : ControllerBase
{
    private readonly INodeRegistry _registry;
    private readonly IEnumerable<ILLMProvider> _llmProviders;

    public NodesController(INodeRegistry registry, IEnumerable<ILLMProvider> llmProviders)
    { _registry = registry; _llmProviders = llmProviders; }

    [HttpGet("catalog")]
    public ActionResult<object> Catalog()
    {
        var descriptors = _registry.GetAllDescriptors().Select(d => new NodeDescriptorResponse(
            d.Type,
            d.DisplayName,
            d.Description,
            d.Category,
            d.Version,
            d.IconKey,
            d.Inputs.Select(i => new NodePortResponse(i.Key, i.DisplayName, i.Description, i.Type.ToString(), i.Required, i.DefaultValue, null)).ToList(),
            d.Outputs.Select(o => new NodePortResponse(o.Key, o.DisplayName, o.Description, o.Type.ToString(), null, null, null)).ToList(),
            d.Configuration.Select(c => new NodePortResponse(c.Key, c.DisplayName, c.Description, c.Type.ToString(), c.Required, c.DefaultValue, c.AllowedValues, c.OptionsSource)).ToList()
        )).ToList();
        return Ok(new { nodes = descriptors });
    }

    /// <summary>Returns all available LLM models from registered providers, for use in dropdown config fields.</summary>
    [HttpGet("models")]
    public ActionResult<object> Models()
    {
        var models = new List<object>
        {
            new { value = "default", label = "default (server configured)" }
        };
        foreach (var provider in _llmProviders)
            foreach (var model in provider.Models)
                models.Add(new { value = $"{provider.Id}/{model}", label = $"{provider.Id} / {model}" });
        return Ok(new { models });
    }

    [HttpGet("/api/health")]
    [AllowAnonymous]
    public ActionResult Health() => Ok(new { status = "ok", uptime = Environment.TickCount64 / 1000 });
}
