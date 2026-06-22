using Confirmai.Services.Payment;
using Confirmai.Services.Payment.Shared;
using Confirmai.Services.Core;
using System.Security.Cryptography;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

public class AbacatePayWebhookServiceTests
{
    [Fact]
    public async Task HandleAsync_MarksEventConfirmationPaid_WhenChargeMatchesPixTxId()
    {
        using var db = TestDataFactory.CreateDbContext();
        db.EventConfirmations.Add(new Confirmai.Models.EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PixTxId = "charge-evt-1",
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, webhookSecret: "expected-secret");
        var body = "{\"event\":\"transparent.completed\",\"data\":{\"id\":\"charge-evt-1\"}}";
        var context = CreateContext(body, "expected-secret");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        var saved = db.EventConfirmations.Single();
        Assert.True(saved.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal("Pix", saved.PaymentGatewayName);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotent_ForEventConfirmationWebhookReplay()
    {
        using var db = TestDataFactory.CreateDbContext();
        db.EventConfirmations.Add(new Confirmai.Models.EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PixTxId = "charge-evt-2",
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, webhookSecret: "expected-secret");
        var body = "{\"event\":\"transparent.completed\",\"data\":{\"id\":\"charge-evt-2\"}}";

        var first = await service.HandleAsync(CreateContext(body, "expected-secret"));
        var second = await service.HandleAsync(CreateContext(body, "expected-secret"));

        Assert.Equal(StatusCodes.Status200OK, Assert.IsAssignableFrom<IStatusCodeHttpResult>(first).StatusCode);
        Assert.Equal(StatusCodes.Status200OK, Assert.IsAssignableFrom<IStatusCodeHttpResult>(second).StatusCode);

        var saved = db.EventConfirmations.Single();
        Assert.True(saved.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal("Pix", saved.PaymentGatewayName);
    }

    private static AbacatePayWebhookService CreateService(Confirmai.Data.AppDbContext db, string webhookSecret)
    {
        var options = Options.Create(new AbacatePayOptions
        {
            WebhookSecret = webhookSecret
        });

        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"abacatepay-webhook-{Guid.NewGuid()}");
        var logService = new LogService(dbFactory, NullLogger<LogService>.Instance);
        var paymentMarker = new WebhookPaymentMarker(db, logService, new PaymentEventBus());

        return new AbacatePayWebhookService(logService, options, paymentMarker);
    }

    private static DefaultHttpContext CreateContext(string body, string webhookSecret)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?webhookSecret={Uri.EscapeDataString(webhookSecret)}");

        var expectedSignature = ComputeHmacBase64(body);
        context.Request.Headers["X-Webhook-Signature"] = expectedSignature;

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Request.Body.Position = 0;

        return context;
    }

    private static string ComputeHmacBase64(string body)
    {
        var keyBytes = Encoding.UTF8.GetBytes(AbacatePayOptions.PublicHmacKey);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToBase64String(hash);
    }
}
