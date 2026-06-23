using Confirmai.Enums;

namespace Confirmai.Tests;

public class PaymentStatusTests
{
    [Fact]
    public void AguardandoPagamento_HasCorrectValue()
    {
        // Assert
        Assert.Equal(0, (int)PaymentStatus.AguardandoPagamento);
    }

    [Fact]
    public void Pago_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)PaymentStatus.Pago);
    }

    [Fact]
    public void AguardandoEntrega_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)PaymentStatus.AguardandoEntrega);
    }

    [Fact]
    public void Entregue_HasCorrectValue()
    {
        // Assert
        Assert.Equal(3, (int)PaymentStatus.Entregue);
    }

    [Fact]
    public void Finalizado_HasCorrectValue()
    {
        // Assert
        Assert.Equal(4, (int)PaymentStatus.Finalizado);
    }

    [Fact]
    public void Cancelado_HasCorrectValue()
    {
        // Assert
        Assert.Equal(5, (int)PaymentStatus.Cancelado);
    }

    [Fact]
    public void Disputa_HasCorrectValue()
    {
        // Assert
        Assert.Equal(6, (int)PaymentStatus.Disputa);
    }

    [Fact]
    public void AguardandoRevisaoAdm_HasCorrectValue()
    {
        // Assert
        Assert.Equal(7, (int)PaymentStatus.AguardandoRevisaoAdm);
    }

    [Fact]
    public void Pendente_HasCorrectValue()
    {
        // Assert
        Assert.Equal(8, (int)PaymentStatus.Pendente);
    }

    [Fact]
    public void Reembolsado_HasCorrectValue()
    {
        // Assert
        Assert.Equal(9, (int)PaymentStatus.Reembolsado);
    }

    [Fact]
    public void Falha_HasCorrectValue()
    {
        // Assert
        Assert.Equal(10, (int)PaymentStatus.Falha);
    }

    [Fact]
    public void AguardandoEntregaInGame_HasCorrectValue()
    {
        // Assert
        Assert.Equal(11, (int)PaymentStatus.AguardandoEntregaInGame);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[]
        {
            PaymentStatus.AguardandoPagamento,
            PaymentStatus.Pago,
            PaymentStatus.AguardandoEntrega,
            PaymentStatus.Entregue,
            PaymentStatus.Finalizado,
            PaymentStatus.Cancelado,
            PaymentStatus.Disputa,
            PaymentStatus.AguardandoRevisaoAdm,
            PaymentStatus.Pendente,
            PaymentStatus.Reembolsado,
            PaymentStatus.Falha,
            PaymentStatus.AguardandoEntregaInGame
        };

        // Assert
        Assert.Equal(12, values.Distinct().Count());
    }
}
