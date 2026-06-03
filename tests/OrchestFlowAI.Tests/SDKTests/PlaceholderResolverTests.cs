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

    [Fact]
    public void Resolve_DottedKey_JsonStringRootWithBoolean_ShouldReturnLowercase()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = "{\"isActive\": true}",
        };

        PlaceholderResolver.Resolve("{{data.isActive}}", inputs).Should().Be("true");
    }

    [Fact]
    public void Resolve_DottedKey_JsonStringRootWithNull_ShouldReturnEmpty()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = "{\"optional\": null}",
        };

        PlaceholderResolver.Resolve("{{data.optional}}", inputs).Should().Be(string.Empty);
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

    [Fact]
    public void Resolve_JsonElementArray_ShouldTraverseWithIndex()
    {
        using var doc = JsonDocument.Parse("[10, 20, 30]");
        var inputs = new Dictionary<string, object?>
        {
            ["nums"] = doc.RootElement.Clone(),
        };

        PlaceholderResolver.Resolve("{{nums.1}}", inputs).Should().Be("20");
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

    [Fact]
    public void Resolve_DottedKeyWithLowerFilter_ShouldApplyFilter()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = "{\"name\":\"HELLO\"}",
        };

        PlaceholderResolver.Resolve("{{obj.name|lower}}", inputs).Should().Be("hello");
    }

    [Fact]
    public void Resolve_DottedKeyWithTrimFilter_ShouldApplyFilter()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = "{\"name\":\"  padded  \"}",
        };

        PlaceholderResolver.Resolve("{{obj.name|trim}}", inputs).Should().Be("padded");
    }

    [Fact]
    public void Resolve_DottedKeyWithDateFilter_ShouldApplyFilter()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = "{\"ts\":\"2026-06-03\"}",
        };

        PlaceholderResolver.Resolve("{{obj.ts|date:yyyy/MM/dd}}", inputs).Should().Be("2026/06/03");
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

    [Fact]
    public void Resolve_ArrayIndex_Negative_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["items"] = "[10, 20]",
        };

        PlaceholderResolver.Resolve("{{items.-1}}", inputs).Should().Be("{{items.-1}}");
    }

    [Fact]
    public void Resolve_ArrayIndex_NonNumeric_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["items"] = "[10, 20]",
        };

        PlaceholderResolver.Resolve("{{items.abc}}", inputs).Should().Be("{{items.abc}}");
    }

    // ── Filters on flat keys ─────────────────────────────────────────────────

    [Fact]
    public void Resolve_DateFilter_ShouldFormatDate()
    {
        var inputs = new Dictionary<string, object?> { ["startDate"] = "2026-01-06" };
        PlaceholderResolver.Resolve("{{startDate|date:yyyy/MM/dd}}", inputs).Should().Be("2026/01/06");
    }

    [Fact]
    public void Resolve_DateFilter_WithTimeComponent_ShouldFormatPreservingDate()
    {
        var inputs = new Dictionary<string, object?> { ["ts"] = "2026-06-03T12:00:00Z" };
        PlaceholderResolver.Resolve("{{ts|date:yyyy-MM-dd}}", inputs).Should().Be("2026-06-03");
    }

    [Fact]
    public void Resolve_DateFilter_UnparseableValue_ShouldReturnRaw()
    {
        var inputs = new Dictionary<string, object?> { ["notDate"] = "hello" };
        PlaceholderResolver.Resolve("{{notDate|date:yyyy}}", inputs).Should().Be("hello");
    }

    [Fact]
    public void Resolve_DateFilter_EmptyFormat_ShouldReturnRaw()
    {
        var inputs = new Dictionary<string, object?> { ["d"] = "2026-01-01" };
        PlaceholderResolver.Resolve("{{d|date:}}", inputs).Should().Be("2026-01-01");
    }

    [Fact]
    public void Resolve_UpperFilter_ShouldUpperCaseValue()
    {
        var inputs = new Dictionary<string, object?> { ["text"] = "hello world" };
        PlaceholderResolver.Resolve("{{text|upper}}", inputs).Should().Be("HELLO WORLD");
    }

    [Fact]
    public void Resolve_LowerFilter_ShouldLowerCaseValue()
    {
        var inputs = new Dictionary<string, object?> { ["text"] = "HELLO World" };
        PlaceholderResolver.Resolve("{{text|lower}}", inputs).Should().Be("hello world");
    }

    [Fact]
    public void Resolve_TrimFilter_ShouldTrimWhitespace()
    {
        var inputs = new Dictionary<string, object?> { ["text"] = "  padded  " };
        PlaceholderResolver.Resolve("{{text|trim}}", inputs).Should().Be("padded");
    }

    [Fact]
    public void Resolve_DefaultFilter_MissingKey_ShouldReturnDefault()
    {
        var inputs = new Dictionary<string, object?>();
        PlaceholderResolver.Resolve("{{missing|default:fallback}}", inputs).Should().Be("fallback");
    }

    [Fact]
    public void Resolve_DefaultFilter_EmptyValue_ShouldReturnDefault()
    {
        var inputs = new Dictionary<string, object?> { ["empty"] = "" };
        PlaceholderResolver.Resolve("{{empty|default:N/A}}", inputs).Should().Be("N/A");
    }

    [Fact]
    public void Resolve_DefaultFilter_PresentValue_ShouldReturnValue()
    {
        var inputs = new Dictionary<string, object?> { ["key"] = "actual" };
        PlaceholderResolver.Resolve("{{key|default:fallback}}", inputs).Should().Be("actual");
    }

    [Fact]
    public void Resolve_UnknownFilter_ShouldPassThroughValue()
    {
        var inputs = new Dictionary<string, object?> { ["x"] = "42" };
        PlaceholderResolver.Resolve("{{x|bogus}}", inputs).Should().Be("42");
    }

    [Fact]
    public void Resolve_FilterWithArgumentContainingColon_ShouldHandleGracefully()
    {
        var inputs = new Dictionary<string, object?> { ["d"] = "2026-06-03T10:00:00" };
        PlaceholderResolver.Resolve("{{d|date:HH:mm:ss}}", inputs).Should().Be("10:00:00");
    }

    // ── Template edge cases ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullTemplate_ShouldReturnEmpty()
    {
        var inputs = new Dictionary<string, object?> { ["x"] = "1" };
        PlaceholderResolver.Resolve(null, inputs).Should().Be(string.Empty);
    }

    [Fact]
    public void Resolve_EmptyTemplate_ShouldReturnEmpty()
    {
        var inputs = new Dictionary<string, object?> { ["x"] = "1" };
        PlaceholderResolver.Resolve(string.Empty, inputs).Should().Be(string.Empty);
    }

    [Fact]
    public void Resolve_NoPlaceholders_ShouldReturnTemplateUnchanged()
    {
        var inputs = new Dictionary<string, object?> { ["x"] = "1" };
        PlaceholderResolver.Resolve("plain text", inputs).Should().Be("plain text");
    }

    [Fact]
    public void Resolve_MultiplePlaceholders_ShouldReplaceAll()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["first"] = "John",
            ["last"] = "Doe",
        };
        PlaceholderResolver.Resolve("{{first}} {{last}}", inputs).Should().Be("John Doe");
    }

    [Fact]
    public void Resolve_SamePlaceholderMultipleTimes_ShouldReplaceAll()
    {
        var inputs = new Dictionary<string, object?> { ["v"] = "X" };
        PlaceholderResolver.Resolve("{{v}}-{{v}}-{{v}}", inputs).Should().Be("X-X-X");
    }

    [Fact]
    public void Resolve_KeyWithWhitespace_ShouldTrimAndResolve()
    {
        var inputs = new Dictionary<string, object?> { ["key"] = "val" };
        PlaceholderResolver.Resolve("{{  key  }}", inputs).Should().Be("val");
    }

    [Fact]
    public void Resolve_WhitespaceInTemplate_ShouldPreserve()
    {
        var inputs = new Dictionary<string, object?> { ["v"] = "X" };
        PlaceholderResolver.Resolve("  before {{v}} after  ", inputs).Should().Be("  before X after  ");
    }

    // ── Null / edge value types ──────────────────────────────────────────────

    [Fact]
    public void Resolve_NullInputValue_ShouldReturnEmptyString()
    {
        var inputs = new Dictionary<string, object?> { ["key"] = null };
        PlaceholderResolver.Resolve("{{key}}", inputs).Should().Be(string.Empty);
    }

    [Fact]
    public void Resolve_NullInputValueWithDefaultFilter_ShouldReturnDefault()
    {
        var inputs = new Dictionary<string, object?> { ["key"] = null };
        PlaceholderResolver.Resolve("{{key|default:fallback}}", inputs).Should().Be("fallback");
    }

    [Fact]
    public void Resolve_IntegerValue_ShouldToString()
    {
        var inputs = new Dictionary<string, object?> { ["count"] = 42 };
        PlaceholderResolver.Resolve("{{count}}", inputs).Should().Be("42");
    }

    [Fact]
    public void Resolve_BooleanTrueValue_ShouldToString()
    {
        var inputs = new Dictionary<string, object?> { ["flag"] = true };
        PlaceholderResolver.Resolve("{{flag}}", inputs).Should().Be("True");
    }

    [Fact]
    public void Resolve_BooleanFalseValue_ShouldToString()
    {
        var inputs = new Dictionary<string, object?> { ["flag"] = false };
        PlaceholderResolver.Resolve("{{flag}}", inputs).Should().Be("False");
    }

    // ── JSON value kinds (via JsonElement) ────────────────────────────────────

    [Fact]
    public void Resolve_JsonElementBooleanTrue_ShouldReturnLowercaseTrue()
    {
        using var doc = JsonDocument.Parse("{\"ready\":true}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.ready}}", inputs).Should().Be("true");
    }

    [Fact]
    public void Resolve_JsonElementBooleanFalse_ShouldReturnLowercaseFalse()
    {
        using var doc = JsonDocument.Parse("{\"done\":false}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.done}}", inputs).Should().Be("false");
    }

    [Fact]
    public void Resolve_JsonElementNull_ShouldReturnEmpty()
    {
        using var doc = JsonDocument.Parse("{\"optional\":null}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.optional}}", inputs).Should().Be(string.Empty);
    }

    [Fact]
    public void Resolve_JsonElementNullWithDefaultFilter_ShouldReturnDefault()
    {
        using var doc = JsonDocument.Parse("{\"optional\":null}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.optional|default:N/A}}", inputs).Should().Be("N/A");
    }

    [Fact]
    public void Resolve_JsonElementInteger_ShouldReturnRawText()
    {
        using var doc = JsonDocument.Parse("{\"count\":42}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.count}}", inputs).Should().Be("42");
    }

    [Fact]
    public void Resolve_JsonElementNegativeNumber_ShouldReturnRawText()
    {
        using var doc = JsonDocument.Parse("{\"temp\":-15.5}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.temp}}", inputs).Should().Be("-15.5");
    }

    [Fact]
    public void Resolve_JsonElementNestedObject_ShouldReturnRawJson()
    {
        using var doc = JsonDocument.Parse("{\"inner\":{\"a\":1,\"b\":2}}");
        var inputs = new Dictionary<string, object?>
        {
            ["data"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{data.inner}}", inputs).Should().Be("{\"a\":1,\"b\":2}");
    }

    [Fact]
    public void Resolve_JsonElementArray_ShouldReturnRawJson()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        var inputs = new Dictionary<string, object?>
        {
            ["arr"] = doc.RootElement.Clone(),
        };
        PlaceholderResolver.Resolve("{{arr}}", inputs).Should().Be("[1,2,3]");
    }

    // ── Object round-trip serialization fallback ─────────────────────────────

    [Fact]
    public void Resolve_DottedKey_PocoObject_ShouldSerializeAndTraverse()
    {
        var poco = new { Name = "Test", Value = 123 };
        var inputs = new Dictionary<string, object?>
        {
            ["obj"] = poco,
        };

        PlaceholderResolver.Resolve("{{obj.Name}}", inputs).Should().Be("Test");
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

        PlaceholderResolver.Resolve("{{scalar.value.deeper}}", inputs).Should().Be("{{scalar.value.deeper}}");
    }

    [Fact]
    public void Resolve_DottedKey_NullRoot_ShouldLeaveUnreplaced()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["root"] = (object?)null,
        };

        PlaceholderResolver.Resolve("{{root.something}}", inputs).Should().Be("{{root.something}}");
    }

    // ── Mixed flat + nested placeholders ─────────────────────────────────────

    [Fact]
    public void Resolve_MixedFlatAndNestedPlaceholders_ShouldReplaceAll()
    {
        var inputs = new Dictionary<string, object?>
        {
            ["name"] = "Greg",
            ["data"] = "{\"store\":\"Jumbo\"}",
        };

        PlaceholderResolver.Resolve("{{name}} went to {{data.store}}", inputs)
            .Should().Be("Greg went to Jumbo");
    }
}
