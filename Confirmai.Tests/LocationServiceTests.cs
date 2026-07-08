using System.Reflection;
using Confirmai.Services;

namespace Confirmai.Tests;

public class LocationServiceTests
{
    private static string InvokeGetStateCode(string stateName)
    {
        var method = typeof(LocationService)
            .GetMethod("GetStateCode", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [stateName])!;
    }

    [Fact]
    public void GetStateCode_ValidStates_ReturnsCorrectCodes()
    {
        Assert.Equal("SP", InvokeGetStateCode("São Paulo"));
        Assert.Equal("RJ", InvokeGetStateCode("Rio de Janeiro"));
        Assert.Equal("MG", InvokeGetStateCode("Minas Gerais"));
        Assert.Equal("RS", InvokeGetStateCode("Rio Grande do Sul"));
    }

    [Fact]
    public void GetStateCode_CaseInsensitive()
    {
        Assert.Equal("SP", InvokeGetStateCode("são paulo"));
        Assert.Equal("RJ", InvokeGetStateCode("RIO DE JANEIRO"));
        Assert.Equal("MG", InvokeGetStateCode("MINAS GERAIS"));
    }

    [Fact]
    public void GetStateCode_InvalidState_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeGetStateCode("XYZ"));
        Assert.Equal(string.Empty, InvokeGetStateCode("Estado Inexistente"));
    }
}
