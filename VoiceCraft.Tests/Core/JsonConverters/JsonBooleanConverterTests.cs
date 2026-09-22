using System.Text.Json;
using VoiceCraft.Core.JsonConverters;
using Xunit;

namespace VoiceCraft.Core.Tests.JsonConverters;

public class JsonBooleanConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonBooleanConverter() }
    };

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"true\"", true)]
    [InlineData("\"false\"", false)]
    public void Read_AcceptsBooleanTokensAndTheirStringForms(string json, bool expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<bool>(json, Options));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("\"True\"")]
    [InlineData("\"yes\"")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Read_RejectsOtherJsonValues(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<bool>(json, Options));
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Write_UsesJsonBooleanTokens(bool value, string expected)
    {
        Assert.Equal(expected, JsonSerializer.Serialize(value, Options));
    }
}
