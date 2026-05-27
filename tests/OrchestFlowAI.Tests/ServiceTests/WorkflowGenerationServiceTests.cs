using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.Api.Services;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.ServiceTests;

public sealed class WorkflowGenerationServiceTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static IWorkflowNodeDescriptor MakeDescriptor(string type, string displayName)
    {
        var mock = new Mock<IWorkflowNodeDescriptor>();
        mock.Setup(d => d.Type).Returns(type);
        mock.Setup(d => d.DisplayName).Returns(displayName);
        mock.Setup(d => d.Description).Returns(string.Empty);
        mock.Setup(d => d.Category).Returns("Test");
        mock.Setup(d => d.Version).Returns("1.0");
        mock.Setup(d => d.IconKey).Returns((string?)null);
        mock.Setup(d => d.Inputs).Returns(Array.Empty<NodeInputDefinition>());
        mock.Setup(d => d.Outputs).Returns(Array.Empty<NodeOutputDefinition>());
        mock.Setup(d => d.Configuration).Returns(Array.Empty<NodeConfigDefinition>());
        return mock.Object;
    }

    private static INodeRegistry MakeRegistry()
    {
        var mock = new Mock<INodeRegistry>();
        mock.Setup(r => r.GetAllDescriptors()).Returns(new List<IWorkflowNodeDescriptor>
        {
            MakeDescriptor("system.start", "Start"),
            MakeDescriptor("system.end", "End"),
            MakeDescriptor("http.request", "HTTP Request"),
        });
        return mock.Object;
    }

    private static LLMProviderRouter MakeRouter(ILLMProvider provider)
    {
        return new LLMProviderRouter(new[] { provider }, "fake", "fake-model");
    }

    private static ILLMProvider MakeLLMProvider(string responseText)
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Id).Returns("fake");
        mock.Setup(p => p.Models).Returns(new[] { "fake-model" });
        mock.Setup(p => p.GenerateTextAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse(responseText, new LLMUsage(10, 20, 30)));
        return mock.Object;
    }

    private static WorkflowGenerationService BuildService(string llmResponse) =>
        new WorkflowGenerationService(
            MakeRouter(MakeLLMProvider(llmResponse)),
            MakeRegistry(),
            NullLogger<WorkflowGenerationService>.Instance);

    // ─── valid response parsing ──────────────────────────────────────────────

    private const string ValidLLMResponse = """
{
  "explanation": "Created a simple HTTP request workflow.",
  "changes": [],
  "definition": {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "My Workflow",
    "version": 1,
    "nodes": [
      {"id": "system.start-1", "type": "system.start", "position": {"x": 350, "y": 150}, "data": {"label": "Start", "config": {}}, "config": {}},
      {"id": "http.request-100", "type": "http.request", "position": {"x": 350, "y": 300}, "data": {"label": "Call API", "config": {"url": "https://example.com"}}, "config": {"url": "https://example.com"}},
      {"id": "system.end-999", "type": "system.end", "position": {"x": 350, "y": 450}, "data": {"label": "End", "config": {}}, "config": {}}
    ],
    "edges": [
      {"id": "edge-system.start-1-http.request-100", "source": "system.start-1", "target": "http.request-100"},
      {"id": "edge-http.request-100-system.end-999", "source": "http.request-100", "target": "system.end-999"}
    ]
  }
}
""";

    [Fact]
    public async Task GenerateAsync_ValidResponse_ReturnsResult()
    {
        var svc = BuildService(ValidLLMResponse);
        var req = new WorkflowGenerationRequest("Call https://example.com", null, "My Workflow");

        var result = await svc.GenerateAsync(req, Guid.NewGuid());

        result.Explanation.Should().Be("Created a simple HTTP request workflow.");
        result.Changes.Should().BeEmpty();
        result.Definition.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_MarkdownFenceStripped_ParsesCorrectly()
    {
        var fenced = $"```json\n{ValidLLMResponse}\n```";
        var svc = BuildService(fenced);
        var req = new WorkflowGenerationRequest("Call https://example.com", null, "My Workflow");

        var result = await svc.GenerateAsync(req, Guid.NewGuid());

        result.Explanation.Should().Be("Created a simple HTTP request workflow.");
    }

    [Fact]
    public async Task GenerateAsync_WithChanges_ReturnsChangeList()
    {
        var withChanges = ValidLLMResponse.Replace(@"""changes"": []", @"""changes"": [""Added HTTP node"", ""Connected edges""]");
        var svc = BuildService(withChanges);
        var req = new WorkflowGenerationRequest("Update the workflow", "{ existing: true }", "My Workflow");

        var result = await svc.GenerateAsync(req, Guid.NewGuid());

        result.Changes.Should().HaveCount(2);
        result.Changes[0].Should().Be("Added HTTP node");
    }

    // ─── error cases ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_InvalidJson_ThrowsInvalidOperationException()
    {
        var svc = BuildService("not valid json");
        var req = new WorkflowGenerationRequest("Do something", null, null);

        var act = () => svc.GenerateAsync(req, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not valid json*");
    }

    [Fact]
    public async Task GenerateAsync_MissingExplanation_Throws()
    {
        const string noExplanation = """{"definition": {"nodes": [{"type": "system.start"}, {"type": "system.end"}], "edges": []}}""";
        var svc = BuildService(noExplanation);
        var req = new WorkflowGenerationRequest("Do something", null, null);

        var act = () => svc.GenerateAsync(req, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*explanation*");
    }

    [Fact]
    public async Task GenerateAsync_MissingSystemStart_Throws()
    {
        const string noStart = """{"explanation": "ok", "changes": [], "definition": {"nodes": [{"type": "system.end"}], "edges": []}}""";
        var svc = BuildService(noStart);
        var req = new WorkflowGenerationRequest("Do something", null, null);

        var act = () => svc.GenerateAsync(req, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*system.start*");
    }

    [Fact]
    public async Task GenerateAsync_MissingSystemEnd_Throws()
    {
        const string noEnd = """{"explanation": "ok", "changes": [], "definition": {"nodes": [{"type": "system.start"}], "edges": []}}""";
        var svc = BuildService(noEnd);
        var req = new WorkflowGenerationRequest("Do something", null, null);

        var act = () => svc.GenerateAsync(req, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*system.end*");
    }

    // ─── prompt building (via LLM request capture) ───────────────────────────

    [Fact]
    public async Task GenerateAsync_NewWorkflow_PromptContainsName()
    {
        LLMRequest? capturedRequest = null;
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.Id).Returns("fake");
        providerMock.Setup(p => p.Models).Returns(new[] { "fake-model" });
        providerMock
            .Setup(p => p.GenerateTextAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LLMRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new LLMResponse(ValidLLMResponse, new LLMUsage(10, 20, 30)));

        var svc = new WorkflowGenerationService(
            MakeRouter(providerMock.Object),
            MakeRegistry(),
            NullLogger<WorkflowGenerationService>.Instance);

        await svc.GenerateAsync(new WorkflowGenerationRequest("Build a webhook", null, "My Webhook Flow"), Guid.NewGuid());

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Prompt.Should().Contain("My Webhook Flow");
        capturedRequest.Prompt.Should().Contain("Build a webhook");
        capturedRequest.SystemPrompt.Should().Contain("system.start");
        capturedRequest.SystemPrompt.Should().Contain("http.request");
    }

    [Fact]
    public async Task GenerateAsync_UpdateWorkflow_PromptContainsCurrentDefinition()
    {
        LLMRequest? capturedRequest = null;
        var providerMock = new Mock<ILLMProvider>();
        providerMock.Setup(p => p.Id).Returns("fake");
        providerMock.Setup(p => p.Models).Returns(new[] { "fake-model" });
        providerMock
            .Setup(p => p.GenerateTextAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LLMRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new LLMResponse(ValidLLMResponse, new LLMUsage(10, 20, 30)));

        var svc = new WorkflowGenerationService(
            MakeRouter(providerMock.Object),
            MakeRegistry(),
            NullLogger<WorkflowGenerationService>.Instance);

        const string existingDef = """{"id":"abc","name":"Old","nodes":[],"edges":[]}""";
        await svc.GenerateAsync(new WorkflowGenerationRequest("Add a delay step", existingDef, null), Guid.NewGuid());

        capturedRequest!.Prompt.Should().Contain("Update the following workflow");
        capturedRequest.Prompt.Should().Contain(existingDef);
        capturedRequest.Prompt.Should().Contain("Add a delay step");
    }
}
