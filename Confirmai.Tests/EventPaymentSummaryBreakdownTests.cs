using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Payment.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xunit;

namespace Confirmai.Tests;

public class EventPaymentSummaryBreakdownTests
{
    private static EventPaymentSummary CreateSummary(EventConfirmation? conf, bool showFeeBreakdown, decimal manualFee = 0.75m)
    {
        var feeOptions = Options.Create(new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            ManualPlatformFeeFixed = manualFee
        });

        var summary = new EventPaymentSummary();
        // Use reflection to set parameters since we can't use bUnit
        var confParam = typeof(EventPaymentSummary).GetProperty("Confirmation");
        confParam!.SetValue(summary, conf);

        var breakdownParam = typeof(EventPaymentSummary).GetProperty("ShowFeeBreakdown");
        breakdownParam!.SetValue(summary, showFeeBreakdown);

        // Set FeeOptions via the inject property
        var feeField = typeof(EventPaymentSummary).GetProperty("FeeOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        feeField!.SetValue(summary, feeOptions);

        return summary;
    }

    private static EventConfirmation CreateConf(decimal price)
    {
        var group = new Group { Name = "Test", EnablePaymentGateways = false, Sport = Sport.Futsal };
        var evt = new Event { Group = group, Price = price, StartsAt = DateTime.UtcNow };
        return new EventConfirmation { Event = evt, Position = FutsalPosition.Outfield };
    }

    [Fact]
    public void GetTotalAmount_WithFeeBreakdown_AddsManualFee()
    {
        var conf = CreateConf(20.0m);
        var summary = CreateSummary(conf, showFeeBreakdown: true);

        var method = typeof(EventPaymentSummary).GetMethod("GetTotalAmount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)method!.Invoke(summary, null)!;

        Assert.Equal(20.75m, result);
    }

    [Fact]
    public void GetTotalAmount_WithoutFeeBreakdown_UsesGatewayFees()
    {
        var conf = CreateConf(20.0m);
        var summary = CreateSummary(conf, showFeeBreakdown: false);

        var method = typeof(EventPaymentSummary).GetMethod("GetTotalAmount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)method!.Invoke(summary, null)!;

        // V2: base + AppFee + GatewayFee = 20 + 0.50 + 0.25 = 20.75
        Assert.Equal(20.75m, result);
    }

    [Fact]
    public void GetTotalAmount_ZeroPrice_ReturnsZero()
    {
        var conf = CreateConf(0m);
        var summary = CreateSummary(conf, showFeeBreakdown: true);

        var method = typeof(EventPaymentSummary).GetMethod("GetTotalAmount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)method!.Invoke(summary, null)!;

        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetTotalAmount_NullPrice_ReturnsZero()
    {
        var conf = new EventConfirmation
        {
            Event = new Event { Price = null, StartsAt = DateTime.UtcNow },
            Position = FutsalPosition.Outfield
        };
        var summary = CreateSummary(conf, showFeeBreakdown: true);

        var method = typeof(EventPaymentSummary).GetMethod("GetTotalAmount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)method!.Invoke(summary, null)!;

        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetTotalAmount_WithFeeBreakdown_ZeroManualFee_ReturnsBaseOnly()
    {
        var conf = CreateConf(20.0m);
        var summary = CreateSummary(conf, showFeeBreakdown: true, manualFee: 0m);

        var method = typeof(EventPaymentSummary).GetMethod("GetTotalAmount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)method!.Invoke(summary, null)!;

        Assert.Equal(20.0m, result);
    }
}
