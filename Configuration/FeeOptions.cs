namespace Confirmai.Configuration;

public class FeeOptions
{
    public const string Section = "Fee";

    /// <summary>
    /// Taxa de serviço em basis points (bps). 100 bps = 1%. Ex: 400 = 4%.
    /// Usar inteiro para evitar problemas de precisão com float.
    /// </summary>
    public int PercentBps { get; set; } = 0;

    /// <summary>Se a taxa de serviço está habilitada.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gateways que suportam cobrança de taxa (split ou repasse).
    /// Gateways não listados não aplicarão taxa.
    /// </summary>
    public string[] SupportedGateways { get; set; } = Array.Empty<string>();

    /// <summary>True quando a taxa está configurada e habilitada.</summary>
    public bool IsConfigured => Enabled && PercentBps > 0;
}
