using System.Reflection;
using System.Runtime.CompilerServices;
using Confirmai.Configuration;
using Confirmai.Pages.Payment;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Tests the UI branching logic of EventPayment for the V1 manual flow (Ciclo 24, Fase 1).
/// ShouldShowGateways and ShouldShowManualPix determine which payment UI sections render.
/// </summary>
public class Ciclo24PaymentUiLogicTests
{
    private static EventPayment CreateInstance(bool groupGatewaysEnabled, bool showDirectPixToOrganizer)
    {
        // Create an instance via reflection (partial class, has many injected deps)
        var instance = (EventPayment)RuntimeHelpers.GetUninitializedObject(typeof(EventPayment))!;

        // Set private field groupGatewaysEnabled
        var gwField = typeof(EventPayment).GetField("groupGatewaysEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
        gwField!.SetValue(instance, groupGatewaysEnabled);

        // Set the FeeOptions inject property
        var feeOptions = new FeeOptions { ShowDirectPixToOrganizer = showDirectPixToOrganizer };
        var optionsWrapper = new OptionsWrapper<FeeOptions>(feeOptions);

        // Find the FeeOptions property (injected via [Inject])
        var feeProp = typeof(EventPayment).GetProperty("FeeOptions", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (feeProp is not null && feeProp.CanWrite)
        {
            feeProp.SetValue(instance, optionsWrapper);
        }
        else
        {
            // Try setting via field if property is get-only
            var feeField = typeof(EventPayment).GetField("<FeeOptions>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            feeField?.SetValue(instance, optionsWrapper);
        }

        return instance;
    }

    [Fact]
    public void ShouldShowGateways_WhenGatewaysDisabled_ReturnsFalse()
    {
        var page = CreateInstance(groupGatewaysEnabled: false, showDirectPixToOrganizer: false);

        Assert.False(page.ShouldShowGateways);
    }

    [Fact]
    public void ShouldShowGateways_WhenGatewaysEnabled_ReturnsTrue()
    {
        var page = CreateInstance(groupGatewaysEnabled: true, showDirectPixToOrganizer: false);

        Assert.True(page.ShouldShowGateways);
    }

    [Fact]
    public void ShouldShowManualPix_WhenGatewaysDisabled_ReturnsTrue()
    {
        var page = CreateInstance(groupGatewaysEnabled: false, showDirectPixToOrganizer: false);

        Assert.True(page.ShouldShowManualPix);
    }

    [Fact]
    public void ShouldShowManualPix_WhenGatewaysEnabledAndDirectPixTrue_ReturnsTrue()
    {
        var page = CreateInstance(groupGatewaysEnabled: true, showDirectPixToOrganizer: true);

        Assert.True(page.ShouldShowManualPix);
    }

    [Fact]
    public void ShouldShowManualPix_WhenGatewaysEnabledAndDirectPixFalse_ReturnsFalse()
    {
        var page = CreateInstance(groupGatewaysEnabled: true, showDirectPixToOrganizer: false);

        Assert.False(page.ShouldShowManualPix);
    }

    [Fact]
    public void V1Manual_BothGatewaysHiddenAndManualPixShown()
    {
        var page = CreateInstance(groupGatewaysEnabled: false, showDirectPixToOrganizer: false);

        Assert.False(page.ShouldShowGateways);
        Assert.True(page.ShouldShowManualPix);
    }
}

/// <summary>
/// Minimal IOptions wrapper for testing.
/// </summary>
internal sealed class OptionsWrapper<T> : IOptions<T> where T : class
{
    public T Value { get; }
    public OptionsWrapper(T value) => Value = value;
}
