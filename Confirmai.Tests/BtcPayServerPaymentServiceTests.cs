using System.Net;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class BtcPayServerPaymentServiceTests
{
    [Fact]
    public async Task GenerateAddressAsync_ReturnsCheckoutLinkAndInvoiceId()
    {
        HttpRequestMessage? capturedRequest = null;

        var service = CreateService(request =>
        {
            capturedRequest = request;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("/api/v1/stores/store-1/invoices", request.RequestUri!.ToString());
            return HttpTestResponses.Json("{\"id\":\"inv-1\",\"checkoutLink\":\"https://btcpay.local/i/inv-1\"}");
        });

        var (address, paymentId) = await service.GenerateAddressAsync(0.00012345m, orderId: "ord-1");

        Assert.Equal("https://btcpay.local/i/inv-1", address);
        Assert.Equal("inv-1", paymentId);
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Authorization?.Scheme == "token");
    }

    [Fact]
    public async Task GetReceivedAmountAsync_ParsesAmountPaid_FromNumberAndString()
    {
        var numericService = CreateService(_ => HttpTestResponses.Json("{\"amountPaid\":0.00003}"));
        var stringService = CreateService(_ => HttpTestResponses.Json("{\"amountPaid\":\"0.00004\"}"));

        var numericValue = await numericService.GetReceivedAmountAsync("inv-1");
        var stringValue = await stringService.GetReceivedAmountAsync("inv-2");

        Assert.Equal(0.00003m, numericValue);
        Assert.Equal(0.00004m, stringValue);
    }

    [Fact]
    public async Task GetReceivedAmountAsync_ReturnsZero_WhenAmountPaidIsMissing()
    {
        var service = CreateService(_ => HttpTestResponses.Json("{\"status\":\"new\"}"));

        var value = await service.GetReceivedAmountAsync("inv-3");

        Assert.Equal(0m, value);
    }

    [Fact]
    public async Task GenerateAddressWithKeyAsync_ThrowsNotImplemented()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<NotImplementedException>(() => service.GenerateAddressWithKeyAsync(0.1m));
    }

    private static BtcPayServerPaymentService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new BtcPayServerPaymentService(
            new StubHttpClientFactory(responder),
            TestConfigurationFactory.Create(
                ("BtcPay:ApiKey", "api-key"),
                ("BtcPay:StoreId", "store-1"),
                ("BtcPay:Url", "https://btcpay.local")));
    }
}

