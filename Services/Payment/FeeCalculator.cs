namespace Confirmai.Services.Payment;

/// <summary>
/// Calculadora de taxa de serviço (por cima).
/// Fórmula: total = round(base * (1 + feePct)) a centavos; feeAmount = total - base
/// </summary>
public record FeeCalculationResult
{
    public decimal BaseAmount { get; init; }
    public decimal FeeAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public int FeePercentBps { get; init; }
}

public class FeeCalculator
{
    private readonly int _feePercentBps;

    public FeeCalculator(int feePercentBps)
    {
        if (feePercentBps < 0)
            throw new InvalidOperationException("Fee percent cannot be negative");
        
        _feePercentBps = feePercentBps;
    }

    /// <summary>
    /// Calcula taxa por cima (jogador paga base + taxa).
    /// </summary>
    /// <param name="baseAmount">Valor base (sem taxa)</param>
    /// <returns>Resultado com base, taxa e total arredondados a centavos</returns>
    public FeeCalculationResult Calculate(decimal baseAmount)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        if (_feePercentBps < 0)
            throw new InvalidOperationException("Fee percent cannot be negative");

        if (_feePercentBps == 0)
        {
            return new FeeCalculationResult
            {
                BaseAmount = baseAmount,
                FeeAmount = 0,
                TotalAmount = baseAmount,
                FeePercentBps = 0
            };
        }

        // Converter bps para decimal (ex: 400 bps = 0.04 = 4%)
        var feeRate = _feePercentBps / 10000m;
        
        // Calcular total com taxa
        var total = baseAmount * (1 + feeRate);
        
        // Arredondar a 2 casas decimais (centavos)
        var totalRounded = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        
        // Derivar feeAmount do total arredondado (evita divergência de 1 centavo)
        var feeAmount = totalRounded - baseAmount;
        
        return new FeeCalculationResult
        {
            BaseAmount = baseAmount,
            FeeAmount = feeAmount,
            TotalAmount = totalRounded,
            FeePercentBps = _feePercentBps
        };
    }
}
