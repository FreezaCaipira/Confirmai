using Confirmai.Enums;
using Confirmai.Services;
using Confirmai.Shared.Helpers;

namespace Confirmai.Tests;

public class OrderStatusHelperTests
{
    [Theory]
    [InlineData(PaymentStatus.AguardandoPagamento, "aguardandopagamento")]
    [InlineData(PaymentStatus.Pendente, "aguardandopagamento")]
    [InlineData(PaymentStatus.Pago, "pago")]
    [InlineData(PaymentStatus.AguardandoEntrega, "aguardandoentrega")]
    [InlineData(PaymentStatus.AguardandoEntregaInGame, "aguardandoentregaingame")]
    [InlineData(PaymentStatus.Entregue, "entregue")]
    [InlineData(PaymentStatus.Finalizado, "finalizado")]
    [InlineData(PaymentStatus.Reembolsado, "finalizado")]
    [InlineData(PaymentStatus.Cancelado, "cancelado")]
    [InlineData(PaymentStatus.Falha, "cancelado")]
    [InlineData(PaymentStatus.Disputa, "disputa")]
    [InlineData(PaymentStatus.AguardandoRevisaoAdm, "aguardandorevisaoadm")]
    public void GetStatusCssClass_ReturnsExpectedClass_ForKnownStatuses(PaymentStatus status, string expected)
    {
        var result = OrderStatusHelper.GetStatusCssClass(status);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetStatusCssClass_ReturnsOutro_ForUnknownStatus()
    {
        var unknown = (PaymentStatus)999;

        var result = OrderStatusHelper.GetStatusCssClass(unknown);

        Assert.Equal("outro", result);
    }

    [Theory]
    [InlineData(PaymentStatus.AguardandoPagamento, "Aguardando pagamento")]
    [InlineData(PaymentStatus.Pago, "Pago")]
    [InlineData(PaymentStatus.AguardandoEntrega, "Aguardando entrega")]
    [InlineData(PaymentStatus.AguardandoEntregaInGame, "Aguardando entrega in-game")]
    [InlineData(PaymentStatus.Entregue, "Entregue")]
    [InlineData(PaymentStatus.Finalizado, "Finalizada")]
    [InlineData(PaymentStatus.Cancelado, "Cancelado")]
    [InlineData(PaymentStatus.Disputa, "Em disputa")]
    [InlineData(PaymentStatus.AguardandoRevisaoAdm, "Aguardando revisao ADM")]
    [InlineData(PaymentStatus.Pendente, "Pendente")]
    [InlineData(PaymentStatus.Reembolsado, "Reembolsado")]
    [InlineData(PaymentStatus.Falha, "Falha")]
    public void GetStatusText_ReturnsExpectedText_ForKnownStatuses(PaymentStatus status, string expected)
    {
        var result = OrderStatusHelper.GetStatusText(status);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetStatusText_ReturnsOutro_ForUnknownStatus()
    {
        var unknown = (PaymentStatus)999;

        var result = OrderStatusHelper.GetStatusText(unknown);

        Assert.Equal("Outro", result);
    }

    [Theory]
    [InlineData("en-US", PaymentStatus.AguardandoRevisaoAdm, "Awaiting admin review")]
    [InlineData("es-ES", PaymentStatus.Finalizado, "Finalizada")]
    [InlineData("pt-BR", PaymentStatus.Disputa, "Em disputa")]
    public void GetStatusText_WithUiTextService_ReturnsLocalizedValue(string languageCode, PaymentStatus status, string expected)
    {
        var language = new LanguagePreferenceService();
        language.SetLanguage(languageCode);
        var textService = new UiTextService(language);

        var result = OrderStatusHelper.GetStatusText(status, textService);

        Assert.Equal(expected, result);
    }
}


