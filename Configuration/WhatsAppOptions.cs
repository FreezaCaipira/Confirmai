namespace Confirmai.Configuration;

/// <summary>
/// C31/C41 — WhatsApp via whatsapp-bridge (whatsmeow), disparo no grupo do
/// WhatsApp do grupo do sistema (nunca DM). Canal secundario: nada critico
/// depende dele.
/// </summary>
public sealed class WhatsAppOptions
{
    /// <summary>Feature flag global. Desligada = nenhum envio e nenhum log.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Base interna do bridge, ex.: http://whatsapp-bridge:8090 (sem dominio publico).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Token Bearer do bridge (BRIDGE_TOKEN) — somente via env var, nunca em appsettings versionado.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default true: o sender registra/loga a mensagem em vez de chamar o
    /// bridge. So vira envio real quando o Robson liberar (DryRun=false).
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// CSV de JIDs de grupo permitidos (...@g.us). Fora de Development,
    /// Enabled=true + DryRun=false + lista vazia falha o boot (guard rail).
    /// </summary>
    public string? AllowedGroupJids { get; set; }

    /// <summary>
    /// URL publica do app (ex.: https://confirmai.com) usada para montar os
    /// links absolutos das mensagens. Vazio = a mensagem vai sem link.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public IReadOnlySet<string> ParseAllowedGroupJids() =>
        (AllowedGroupJids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
