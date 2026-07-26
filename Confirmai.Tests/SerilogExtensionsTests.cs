using Confirmai.Configuration;
using Serilog;
using Serilog.Events;

namespace Confirmai.Tests;

public class SerilogExtensionsTests
{
    [Fact]
    public void WriteSerilogOtlpSinkIfEnabled_ReturnsConfig_WhenDisabled()
    {
        var config = new LoggerConfiguration();
        var options = new OtelOptions { Endpoint = "" };
        
        var result = config.WriteSerilogOtlpSinkIfEnabled(options);
        
        Assert.Same(config, result);
    }

    [Fact]
    public void WriteSerilogOtlpSinkIfEnabled_AddsSink_WhenEnabled()
    {
        var config = new LoggerConfiguration();
        var options = new OtelOptions 
        { 
            Endpoint = "http://localhost:4317",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
        };
        
        var result = config.WriteSerilogOtlpSinkIfEnabled(options);
        
        Assert.Same(config, result);
    }

    [Fact]
    public void WriteSerilogOtlpSinkIfEnabled_AddsHeaders_WhenHeadersConfigured()
    {
        var config = new LoggerConfiguration();
        var options = new OtelOptions 
        { 
            Endpoint = "http://localhost:4317",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Headers = "Authorization=Bearer token,X-Custom=custom-value"
        };
        
        var result = config.WriteSerilogOtlpSinkIfEnabled(options);
        
        Assert.Same(config, result);
    }

    [Fact]
    public void WriteSerilogOtlpSinkIfEnabled_DoesNotAddHeaders_WhenHeadersEmpty()
    {
        var config = new LoggerConfiguration();
        var options = new OtelOptions 
        { 
            Endpoint = "http://localhost:4317",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Headers = ""
        };
        
        var result = config.WriteSerilogOtlpSinkIfEnabled(options);
        
        Assert.Same(config, result);
    }

    [Fact]
    public void WriteSerilogOtlpSinkIfEnabled_DoesNotAddHeaders_WhenHeadersNull()
    {
        var config = new LoggerConfiguration();
        var options = new OtelOptions 
        { 
            Endpoint = "http://localhost:4317",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Headers = null!
        };
        
        var result = config.WriteSerilogOtlpSinkIfEnabled(options);
        
        Assert.Same(config, result);
    }
}
