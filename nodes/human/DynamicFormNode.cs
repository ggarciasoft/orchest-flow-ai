using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Text.Json;

namespace OrchestFlowAI.Nodes.Human;

/// <summary>
/// A dynamically-generated workflow node that pauses execution and presents a custom form to the user.
/// On the first execution pass it suspends with WaitingForApproval; on resume (when _formSubmitted is present)
/// it propagates the submitted values as outputs.
/// </summary>
public sealed class DynamicFormNode : IWorkflowNode
{
    private readonly Form _form;
    /// <summary>The version number of the active FormVersion snapshot used by this node instance.</summary>
    private readonly int? _activeVersionNumber;

    public string Type => $"form.{_form.Slug}";

    /// <param name="form">The form entity. FieldsJson must reflect the active version's fields.</param>
    /// <param name="activeVersionNumber">Version number of the active FormVersion — stored in the approval payload so the UI can display the exact version used at execution time.</param>
    public DynamicFormNode(Form form, int? activeVersionNumber = null)
    {
        _form = form;
        _activeVersionNumber = activeVersionNumber;
    }

    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var fields = JsonSerializer.Deserialize<List<FormFieldDefinition>>(_form.FieldsJson ?? "[]",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        // Resume path: engine calls ExecuteAsync again after form is submitted.
        if (ctx.NodeInputs.TryGetValue("_formSubmitted", out var submitted) && submitted is true or "true")
        {
            var outputs = new Dictionary<string, object?>();
            foreach (var field in fields)
                if (ctx.NodeInputs.TryGetValue(field.Key, out var val))
                    outputs[field.Key] = val;
            outputs["_formSubmitted"] = true;
            return Task.FromResult(NodeExecutionResult.Succeeded(outputs));
        }

        // Not yet submitted — pause and wait, embedding the version number used at this moment
        var payload = new Dictionary<string, object?>
        {
            ["_formId"] = _form.Id.ToString(),
            ["_formSlug"] = _form.Slug,
            ["_formName"] = _form.Name,
            ["_formFields"] = _form.FieldsJson,
        };
        if (_activeVersionNumber.HasValue)
            payload["_formVersionNumber"] = _activeVersionNumber.Value;

        return Task.FromResult(NodeExecutionResult.WaitingForApproval(payload));
    }
}

/// <summary>Descriptor for <see cref="DynamicFormNode"/> — provides metadata for the designer palette.</summary>
public sealed class DynamicFormNodeDescriptor : IWorkflowNodeDescriptor
{
    private readonly Form _form;

    public DynamicFormNodeDescriptor(Form form) { _form = form; }

    public string Type => $"form.{_form.Slug}";
    public string DisplayName => _form.Name;
    public string Description => _form.Description ?? $"Custom form: {_form.Name}";
    public string Category => "forms";
    public string Version => "1.0.0";
    public string? IconKey => "clipboard-list";

    public IReadOnlyCollection<NodeInputDefinition> Inputs =>
        ParseFields()
            .Select(f => new NodeInputDefinition(f.Key, f.Label, f.Placeholder ?? f.Label, DataType.String, f.Required))
            .ToList();

    public IReadOnlyCollection<NodeOutputDefinition> Outputs =>
        ParseFields()
            .Select(f => new NodeOutputDefinition(f.Key, f.Label, $"User-submitted value for {f.Label}", DataType.String))
            .Append(new NodeOutputDefinition("_formSubmitted", "Form Submitted", "True when the form was submitted.", DataType.Boolean))
            .ToList();

    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();

    private List<FormFieldDefinition> ParseFields() =>
        JsonSerializer.Deserialize<List<FormFieldDefinition>>(_form.FieldsJson ?? "[]",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
}
