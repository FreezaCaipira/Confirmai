namespace Confirmai.Configuration;

public class FeeOptions
{
    public const string Section = "Fee";

    /// <summary>Taxa fixa do app em reais.</summary>
    public decimal AppFeeFixed { get; set; } = 0;

    /// <summary>Taxa fixa do gateway em reais.</summary>
    public decimal GatewayFeeFixed { get; set; } = 0;

    /// <summary>Se a taxa de serviço está habilitada.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gateways que suportam cobrança de taxa (split ou repasse).
    /// Gateways não listados não aplicarão taxa.
    /// </summary>
    public string[] SupportedGateways { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Se deve exibir a opção de Pix direto ao organizador.
    /// Quando false, apenas os gateways configurados serão exibidos.
    /// </summary>
    public bool ShowDirectPixToOrganizer { get; set; } = true;

    /// <summary>
    /// Taxa fixa da plataforma no fluxo MANUAL (V1), em reais.
    /// Aplicada apenas em partidas de futsal com preço > 0 em grupos sem gateway.
    /// Não confundir com AppFeeFixed/GatewayFeeFixed (matemática do V2 com gateway).
    /// </summary>
    public decimal ManualPlatformFeeFixed { get; set; } = 0;

    /// <summary>
    /// Chave Pix da plataforma para recebimento do repasse da taxa (fluxo manual V1).
    /// O organizador transfere para esta chave e anexa o comprovante.
    /// Vazia quando não configurada — a UI mostra aviso.
    /// </summary>
    public string PlatformPixKey { get; set; } = string.Empty;

    /// <summary>Cidade da plataforma para o payload Pix estático (campo 60 do BR Code).</summary>
    public string? PlatformPixCity { get; set; }

    /// <summary>True quando a taxa está configurada e habilitada.</summary>
    public bool IsConfigured => Enabled && (AppFeeFixed > 0 || GatewayFeeFixed > 0);
}
