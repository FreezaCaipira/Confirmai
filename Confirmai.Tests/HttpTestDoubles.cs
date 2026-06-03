using System.Net;
using System.Text;

namespace Confirmai.Tests;

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new RoutingHandler(_responder));
    }
}

internal sealed class RoutingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responder(request));
    }
}

internal static class HttpTestResponses
{
    public static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

