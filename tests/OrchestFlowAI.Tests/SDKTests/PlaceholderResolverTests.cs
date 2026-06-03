using System.Text.Json;
using FluentAssertions;
using OrchestFlowAI.SDK.Helpers;

namespace OrchestFlowAI.Tests.SDKTests;

public sealed class PlaceholderResolverTests
{
    // ── Flat key (unchanged behaviour) ──────────────────────────────────────

    [Fact]
    public void Resolve_FlatKey_ShouldReturnValue()
    {
        var inputs = new Dictionary<string, object?> { ["name"] = "Alice" };
        PlaceholderResolver.Resolve("Hello {{name}}!", inputs).Should().Be("Hello Alice!");
    }

    [Fact]
    public void Resolve_MissingFlatKey_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>();
        PlaceholderResolver.Resolve("{{missing}}", inputs).Should().Be("{{missing}}");
    }

    [Fact]
    public void Resolve_FlatDottedKey_ExactMatchShouldWinOverNestedResolution()
    {
        // 'a.b' exists as a flat key — nested resolution must not be attempted.
        var inputs = new Dictionary<string, object?>
        {
            ["a.b"] = "flat-value",
            ["a"]   = "{\"b\":\"nested-value\"}",
        };

        PlaceholderResolver.Resolve("{{a.b}}", inputs).Should().Be("flat-value");
    }

    // ── Nested: JSON string root ─────────────────────────────────────────────

    [Fact]
    public void Resolve_DottedKey_JsonStringRoot_ShouldReturnNestedValue()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["extractedJson"] = "{\"Amount\": 2988.00}",
        };

        PlaceholderResolver.Resolve("{{extractedJson.Amount}}", inputs).Should().Be("2988.00");
    }

    [Fact]
    public void Resolve_DottedKey_JsonStringRootWithStringProperty_ShouldReturnNestedValue()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["extractedJson"] = "{\"Currency\": \"USD\", \"Amount\": 2988.00}",
        };

        PlaceholderResolver.Resolve("{{extractedJson.Currency}}", inputs).Should().Be("USD");
    }

    // ── Nested: JsonElement root ─────────────────────────────────────────────

    [Fact]
    public void Resolve_DottedKey_JsonElementRoot_ShouldReturnNestedValue()
    {
        using var doc = JsonDocument.Parse("{\"deep\":{\"value\":\"treasure\"}}");
        var inputs = new Dictionary<string, object?>
        {
            ["nested"] = doc.RootElement.Clone(),
        };

        PlaceholderResolver.Resolve("{{nested.deep.value}}", inputs).Should().Be("treasure");
    }

    [Fact]
    public void Resolve_MultiLevelDottedKey_JsonElement_ShouldTraverseAllLevels()
    {
        using var doc = JsonDocument.Parse("{\"a\":{\"b\":{\"c\":\"deep\"}}}");
        var inputs = new Dictionary<string, object?>
        {
            ["root"] = doc.RootElement.Clone(),
        };

        PlaceholderResolver.Resolve("{{root.a.b.c}}", inputs).Should().Be("deep");
    }

    // ── Nested with filter ────────────────────────────────────────────────────

    [Fact]
    public void Resolve_DottedKeyWithDefaultFilter_MissingPath_ShouldReturnDefault()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = "{\"info\":{}}",
        };

        PlaceholderResolver.Resolve("{{data.info.price|default:0}}", inputs).Should().Be("0");
    }

    [Fact]
    public void Resolve_DottedKeyWithDefaultFilter_PresentPath_ShouldReturnValue()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = "{\"info\":{\"price\":\"99.99\"}}",
        };

        PlaceholderResolver.Resolve("{{data.info.price|default:0}}", inputs).Should().Be("99.99");
    }

    [Fact]
    public void Resolve_DottedKeyWithUpperFilter_ShouldApplyFilter()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = "{\"name\":\"hello\"}",
        };

        PlaceholderResolver.Resolve("{{obj.name|upper}}", inputs).Should().Be("HELLO");
    }

    // ── Array index access ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_ArrayIndex_ShouldReturnIndexedElement()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["items"] = "[{\"name\":\"first\"},{\"name\":\"second\"}]",
        };

        PlaceholderResolver.Resolve("{{items.0.name}}", inputs).Should().Be("first");
    }

    [Fact]
    public void Resolve_ArrayIndex_SecondElement_ShouldReturnCorrectValue()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["items"] = "[{\"name\":\"first\"},{\"name\":\"second\"}]",
        };

        PlaceholderResolver.Resolve("{{items.1.name}}", inputs).Should().Be("second");
    }

    [Fact]
    public void Resolve_ArrayIndex_OutOfBounds_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["items"] = "[{\"name\":\"only\"}]",
        };

        PlaceholderResolver.Resolve("{{items.5.name}}", inputs).Should().Be("{{items.5.name}}");
    }

    // ── Invalid / error paths ────────────────────────────────────────────────

    [Fact]
    public void Resolve_DottedKey_InvalidJson_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["broken"] = "not-json",
        };

        PlaceholderResolver.Resolve("{{broken.field}}", inputs).Should().Be("{{broken.field}}");
    }

    [Fact]
    public void Resolve_DottedKey_MissingProperty_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = "{\"a\":1}",
        };

        PlaceholderResolver.Resolve("{{obj.nonexistent}}", inputs).Should().Be("{{obj.nonexistent}}");
    }

    [Fact]
    public void Resolve_DottedKey_RootMissing_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>();
        PlaceholderResolver.Resolve("{{missing.field}}", inputs).Should().Be("{{missing.field}}");
    }

    [Fact]
    public void Resolve_DottedKey_TraversalOnScalar_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["scalar"] = "{\"value\":42}",
        };

        // value is a number; trying to traverse deeper should fail gracefully.
        PlaceholderResolver.Resolve("{{scalar.value.deeper}}", inputs).Should().Be("{{scalar.value.deeper}}");
    }
}
