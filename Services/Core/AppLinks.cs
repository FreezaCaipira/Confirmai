using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Core;

/// <summary>
/// C40 F1 — montador canonico de links absolutos do app para canais externos
/// (email, WhatsApp). A base vem de <c>WhatsApp__PublicBaseUrl</c> (config
/// existente, continua valendo) e cai em <c>App__PublicBaseUrl</c> quando
/// aquela esta vazia. Sem base configurada devolve <c>null</c> — os chamadores
/// omitem o link; URL relativa nunca sai em canal externo.
/// </summary>
public sealed class AppLinks
{
    private readonly string? _baseUrl;

    public AppLinks(IOptions<AppOptions> app, IOptions<WhatsAppOptions> whatsApp)
        : this(ResolveBaseUrl(app.Value, whatsApp.Value))
    {
    }

    private AppLinks(string? baseUrl)
    {
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim().TrimEnd('/');
    }

    private static string? ResolveBaseUrl(AppOptions app, WhatsAppOptions whatsApp)
        => !string.IsNullOrWhiteSpace(whatsApp.PublicBaseUrl)
            ? whatsApp.PublicBaseUrl
            : app.PublicBaseUrl;

    /// <summary>Pagina da partida: /poker/{id} ou /futsal/{id}.</summary>
    public string? EventUrl(Event ev)
    {
        if (_baseUrl is null) return null;
        var path = ev.Sport == Sport.Poker ? "poker" : "futsal";
        return $"{_baseUrl}/{path}/{ev.Id}";
    }

    /// <summary>Hub do grupo: /grupo/{id}.</summary>
    public string? GroupUrl(int groupId)
        => _baseUrl is null ? null : $"{_baseUrl}/grupo/{groupId}";

    /// <summary>Aba de pagamentos do grupo: /grupo/{id}/pagamentos.</summary>
    public string? GroupPaymentsUrl(int groupId)
        => _baseUrl is null ? null : $"{_baseUrl}/grupo/{groupId}/pagamentos";
}
