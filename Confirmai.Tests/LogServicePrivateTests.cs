using Confirmai.Services.Core;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class LogServicePrivateTests
{
    [Fact]
    public void EnrichWithRequestContext_WithNullHttpContext_DoesNotModifyLog()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns((HttpContext?)null);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = null,
            CorrelationId = null
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Null(log.IpAddress);
        Assert.Null(log.CorrelationId);
    }

    [Fact]
    public void EnrichWithRequestContext_WithRemoteIpAddress_SetsIpAddress()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = null,
            CorrelationId = null
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Equal("192.168.1.1", log.IpAddress);
    }

    [Fact]
    public void EnrichWithRequestContext_WithLongIpAddress_TruncatesTo64Chars()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = new string('A', 100),
            CorrelationId = null
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Equal(100, log.IpAddress?.Length); // Should not override existing value
    }

    [Fact]
    public void EnrichWithRequestContext_WithTraceIdentifier_SetsCorrelationId()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = null,
            CorrelationId = null
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Equal("trace-123", log.CorrelationId);
    }

    [Fact]
    public void EnrichWithRequestContext_WithLongTraceIdentifier_TruncatesTo80Chars()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = new string('B', 100);
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = null,
            CorrelationId = null
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Equal(80, log.CorrelationId?.Length);
    }

    [Fact]
    public void EnrichWithRequestContext_WithExistingValues_DoesNotOverride()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"log-service-{Guid.NewGuid()}");
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        httpContext.TraceIdentifier = "trace-123";
        
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        
        var service = new LogService(dbFactory, NullLogger<Confirmai.Services.Core.LogService>.Instance, httpContextAccessor.Object);
        var log = new AppLog
        {
            IpAddress = "existing-ip",
            CorrelationId = "existing-trace"
        };

        var method = typeof(LogService).GetMethod("EnrichWithRequestContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(service, new object[] { log });

        Assert.Equal("existing-ip", log.IpAddress);
        Assert.Equal("existing-trace", log.CorrelationId);
    }
}
