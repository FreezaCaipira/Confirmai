using Confirmai.Configuration;
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

    /// <summary>True quando o grupo está isento neste instante (prazo exclusivo, UTC).</summary>
    public bool IsWaived(Group group, DateTime nowUtc)
        => group.PlatformFeeWaivedUntil is DateTime until && until > nowUtc;

    /// <summary>
    /// Taxa manual efetiva para o grupo: 0 quando isento, a taxa fixa configurada
    /// caso contrário. A taxa isenta continua sendo carimbada (como 0) no snapshot
    /// da confirmação — nunca é pulada — para que a métrica de isenção exista.
    /// Grupo desconhecido (navigation não carregada) cai na taxa configurada.
    /// </summary>
    public decimal ResolveManualFee(Group? group, DateTime nowUtc)
        => group is not null && IsWaived(group, nowUtc) ? 0m : _feeOptions.Value.ManualPlatformFeeFixed;
}
