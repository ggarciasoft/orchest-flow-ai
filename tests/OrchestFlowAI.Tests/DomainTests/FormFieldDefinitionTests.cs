using FluentAssertions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class FormFieldDefinitionTests
{
    // ── Construction ────────────────────────────────────────────────────────

    [Fact]
    public void Create_MinimalFields_DefaultsAreCorrect()
    {
        var field = new FormFieldDefinition("name", "Full Name", "text");

        field.Key.Should().Be("name");
        field.Label.Should().Be("Full Name");
        field.Type.Should().Be("text");
        field.Required.Should().BeFalse();
        field.Placeholder.Should().BeNull();
        field.Options.Should().BeNull();
        field.OptionsFrom.Should().BeNull();
        field.ValidationRegex.Should().BeNull();
        field.ValidationMessage.Should().BeNull();
        field.Accept.Should().BeNull();
    }

    [Fact]
    public void Create_FileField_AcceptIsStored()
    {
        var field = new FormFieldDefinition(
            Key: "invoice",
            Label: "Invoice PDF",
            Type: "file",
            Required: true,
            Accept: ".pdf,application/pdf");

        field.Type.Should().Be("file");
        field.Required.Should().BeTrue();
        field.Accept.Should().Be(".pdf,application/pdf");
    }

    [Fact]
    public void Create_FileField_NoAccept_AcceptIsNull()
    {
        var field = new FormFieldDefinition("attachment", "Attachment", "file");

        field.Type.Should().Be("file");
        field.Accept.Should().BeNull();
    }

    [Fact]
    public void Create_SelectField_OptionsAndRequired()
    {
        var options = new[] { "Red", "Green", "Blue" };
        var field = new FormFieldDefinition(
            Key: "color",
            Label: "Color",
            Type: "select",
            Required: true,
            Options: options);

        field.Options.Should().BeEquivalentTo(options);
        field.Required.Should().BeTrue();
    }

    [Fact]
    public void Create_SelectField_WithOptionsFrom()
    {
        var field = new FormFieldDefinition(
            Key: "category",
            Label: "Category",
            Type: "select",
            OptionsFrom: "categories");

        field.OptionsFrom.Should().Be("categories");
        field.Options.Should().BeNull();
    }

    [Fact]
    public void Create_TextField_WithValidationRegex()
    {
        var field = new FormFieldDefinition(
            Key: "amount",
            Label: "Amount",
            Type: "number",
            ValidationRegex: @"^\d+(\.\d{1,2})?$",
            ValidationMessage: "Enter a valid decimal");

        field.ValidationRegex.Should().Be(@"^\d+(\.\d{1,2})?$");
        field.ValidationMessage.Should().Be("Enter a valid decimal");
    }

    // ── Type coverage ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("text")]
    [InlineData("number")]
    [InlineData("select")]
    [InlineData("date")]
    [InlineData("email")]
    [InlineData("boolean")]
    [InlineData("file")]
    public void AllFieldTypes_CanBeInstantiated(string type)
    {
        var field = new FormFieldDefinition("key", "Label", type);
        field.Type.Should().Be(type);
    }
}
