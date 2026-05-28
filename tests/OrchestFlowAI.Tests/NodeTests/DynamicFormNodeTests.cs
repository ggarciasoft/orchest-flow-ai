using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Nodes.Human;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

public sealed class DynamicFormNodeTests
{
    private static Form CreateTestForm(string fieldsJson = "") =>
        Form.Create(
            Guid.NewGuid(),
            "Expense Review",
            "expense-review",
            "Review expense claims",
            fieldsJson.Length > 0
                ? fieldsJson
                : """[{"Key":"amount","Label":"Amount","Type":"number","Required":true},{"Key":"notes","Label":"Notes","Type":"text","Required":false}]""");

    // ────────────────────────────────────────────────────────────────────────
    // First execution — no inputs yet
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_NoInputs_ReturnsWaitingForApproval()
    {
        var form = CreateTestForm();
        var node = new DynamicFormNode(form);

        var ctx = new TestContextBuilder()
            .WithInputs(new Dictionary<string, object?>())
            .Build();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        result.Outputs.Should().ContainKey("_formId");
        result.Outputs.Should().ContainKey("_formSlug");
        result.Outputs["_formSlug"].Should().Be("expense-review");
        result.Outputs.Should().ContainKey("_formFields");
    }

    [Fact]
    public async Task Execute_MissingRequiredField_ReturnsWaitingForApproval()
    {
        var form = CreateTestForm();
        var node = new DynamicFormNode(form);

        // "notes" is optional, "amount" is required but missing
        var ctx = new TestContextBuilder()
            .WithInputs(new Dictionary<string, object?> { ["notes"] = "some note" })
            .Build();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Resume path — required fields present
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AllRequiredFieldsPresent_ReturnsSucceeded()
    {
        var form = CreateTestForm();
        var node = new DynamicFormNode(form);

        var ctx = new TestContextBuilder()
            .WithInputs(new Dictionary<string, object?>
            {
                ["amount"] = "150.00",
                ["notes"] = "Travel expenses"
            })
            .Build();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("amount");
        result.Outputs["amount"].Should().Be("150.00");
        result.Outputs.Should().ContainKey("_formSubmitted");
        result.Outputs["_formSubmitted"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_OnlyRequiredFieldsPresent_SucceedsWithoutOptional()
    {
        var form = CreateTestForm();
        var node = new DynamicFormNode(form);

        // Only required "amount" provided, optional "notes" omitted
        var ctx = new TestContextBuilder()
            .WithInputs(new Dictionary<string, object?> { ["amount"] = "99.99" })
            .Build();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("amount");
        result.Outputs.Should().NotContainKey("notes");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Edge cases
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_NoFields_ReturnsWaitingForApproval()
    {
        // A form with no fields — empty form still waits (fields.Count == 0)
        var form = CreateTestForm("[]");
        var node = new DynamicFormNode(form);

        var ctx = new TestContextBuilder()
            .WithInputs(new Dictionary<string, object?>())
            .Build();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        // fields.Any() is false → WaitingForApproval
        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
    }

    [Fact]
    public void Node_Type_MatchesFormSlug()
    {
        var form = CreateTestForm();
        var node = new DynamicFormNode(form);
        node.Type.Should().Be("form.expense-review");
    }

    [Fact]
    public void Descriptor_Inputs_ContainFormFields()
    {
        var form = CreateTestForm();
        var descriptor = new DynamicFormNodeDescriptor(form);
        descriptor.Inputs.Should().Contain(i => i.Key == "amount");
        descriptor.Inputs.Should().Contain(i => i.Key == "notes");
    }

    [Fact]
    public void Descriptor_Outputs_ContainFormFieldsPlusSubmittedFlag()
    {
        var form = CreateTestForm();
        var descriptor = new DynamicFormNodeDescriptor(form);
        descriptor.Outputs.Should().Contain(o => o.Key == "amount");
        descriptor.Outputs.Should().Contain(o => o.Key == "_formSubmitted");
    }
}
