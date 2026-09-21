using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Regra única de resolução da taxa de plataforma do fluxo manual (V1).
/// Nenhum código deve ler <see cref="FeeOptions.ManualPlatformFeeFixed"/>
/// diretamente: a isenção por grupo (<see cref="Group.PlatformFeeWaivedUntil"/>)
/// zera a taxa enquanto estiver dentro do prazo.
/// </summary>
public sealed class PlatformFeePolicy
{
    private readonly IOptions<FeeOptions> _feeOptions;

    public PlatformFeePolicy(IOptions<FeeOptions> feeOptions)
    {
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
    }

    /// <summary>
    /// True quando <paramref name="asOfUtc"/> cai dentro da janela de isenção
    /// [WaivedFrom, WaivedUntil). WaivedFrom nulo (dados antigos) = "desde sempre".
    /// </summary>
    public bool IsWaived(Group group, DateTime asOfUtc)
        => group.PlatformFeeWaivedUntil is DateTime until && until > asOfUtc
           && (group.PlatformFeeWaivedFrom is not DateTime from || from <= asOfUtc);

    /// <summary>
    /// Taxa manual efetiva para o grupo: 0 quando isento, a taxa fixa configurada
    /// caso contrário. A taxa isenta continua sendo carimbada (como 0) no snapshot
    /// da confirmação — nunca é pulada — para que a métrica de isenção exista.
    /// Grupo desconhecido (navigation não carregada) cai na taxa configurada.
    /// </summary>
    public decimal ResolveManualFee(Group? group, DateTime nowUtc)
        => group is not null && IsWaived(group, nowUtc) ? 0m : _feeOptions.Value.ManualPlatformFeeFixed;

    /// <summary>O fluxo manual V1 cobra taxa: futsal, sem gateways, preco > 0.</summary>
    public static bool AppliesTo(Group? group, decimal? price)
        => group is { Sport: Sport.Futsal, EnablePaymentGateways: false }
           && price.GetValueOrDefault() > 0m;

    /// <summary>
    /// Carimbo na criacao da confirmacao (C36-C Fase 0 — ressalva de dinheiro
    /// do C34): a taxa e resolvida UMA vez, aqui. QR, resumo do organizador e
    /// stamp do repasse leem o valor carimbado — a isencao concedida depois
    /// nao retroage, e a expirada depois nao surpreende. Fora do fluxo manual
    /// (poker, gateways, gratis) devolve null e o stamp tardio decide.
    /// </summary>
    public decimal? ResolveStampForNewConfirmation(Group? group, decimal? price, DateTime nowUtc)
        => AppliesTo(group, price) ? ResolveManualFee(group, nowUtc) : null;
}
