using Confirmai.Configuration;

namespace Confirmai.Tests;

public class OtelOptionsTests
{
    [Fact]
    public void IsEnabled_WhenEndpointIsEmpty_ReturnsFalse()
    {
        // Arrange
        var options = new OtelOptions { Endpoint = string.Empty };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenEndpointIsWhitespace_ReturnsFalse()
    {
        // Arrange
        var options = new OtelOptions { Endpoint = "   " };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenEndpointHasValue_ReturnsTrue()
    {
        // Arrange
        var options = new OtelOptions { Endpoint = "https://otlp.example.com" };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersEmpty_ReturnsEmptyDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = string.Empty };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Empty(headers);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersWhitespace_ReturnsEmptyDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "   " };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Empty(headers);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasSinglePair_ReturnsDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization=Basic xyz" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal("Basic xyz", headers["Authorization"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasMultiplePairs_ReturnsDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization=Basic xyz,X-Scope-OrgId=my-org" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Equal(2, headers.Count);
        Assert.Equal("Basic xyz", headers["Authorization"]);
        Assert.Equal("my-org", headers["X-Scope-OrgId"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasSpacesAroundCommas_ReturnsDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization=Basic xyz , X-Scope-OrgId=my-org" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Equal(2, headers.Count);
        Assert.Equal("Basic xyz", headers["Authorization"]);
        Assert.Equal("my-org", headers["X-Scope-OrgId"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasSpacesAroundEquals_ReturnsDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization = Basic xyz" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal("Basic xyz", headers["Authorization"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasPairWithoutValue_ReturnsDictionaryWithEmptyValue()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization=" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal(string.Empty, headers["Authorization"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasPairWithoutEquals_ReturnsDictionaryWithoutPair()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Authorization,Key=Value" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal("Value", headers["Key"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasEmptyPairs_ReturnsDictionaryWithoutEmptyPairs()
    {
        // Arrange
        var options = new OtelOptions { Headers = ",," };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Empty(headers);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersHasDuplicateKeys_ReturnsLastValue()
    {
        // Arrange
        var options = new OtelOptions { Headers = "Key=Value1,Key=Value2" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal("Value2", headers["Key"]);
    }

    [Fact]
    public void ParsedHeaders_WhenHeadersIsCaseInsensitive_ReturnsDictionary()
    {
        // Arrange
        var options = new OtelOptions { Headers = "authorization=Basic xyz" };

        // Act
        var headers = options.ParsedHeaders();

        // Assert
        Assert.Single(headers);
        Assert.Equal("Basic xyz", headers["AUTHORIZATION"]);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new OtelOptions();

        // Assert
        Assert.Equal(string.Empty, options.Endpoint);
        Assert.Equal(string.Empty, options.Headers);
        Assert.Equal("Confirmai", options.ServiceName);
        Assert.Equal("1.0.0", options.ServiceVersion);
        Assert.Equal("production", options.Environment);
    }
}
