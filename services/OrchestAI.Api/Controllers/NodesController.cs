using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestAI.Contracts.Responses;
using OrchestAI.Engine.Registry;
using OrchestAI.SDK.Interfaces;

namespace OrchestAI.Api.Controllers;

[ApiController, Route("api/nodes"), Authorize]
public sealed class NodesController : ControllerBase
{
    private readonly INodeRegistry _registry;
    public NodesController(INodeRegistry registry) => _registry = registry;

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
            d.Configuration.Select(c => new NodePortResponse(c.Key, c.DisplayName, c.Description, c.Type.ToString(), c.Required, c.DefaultValue, c.AllowedValues)).ToList()
        )).ToList();
        return Ok(new { nodes = descriptors });
    }

    [HttpGet("/api/health")]
    [AllowAnonymous]
    public ActionResult Health() => Ok(new { status = "ok", uptime = Environment.TickCount64 / 1000 });
}
