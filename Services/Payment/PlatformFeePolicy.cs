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

    /// <summary>
    /// O fluxo manual V1 cobra taxa: futsal ou poker, sem gateways, preco > 0
    /// (C39-A — o poker passa a cobrar pelo app).
    /// </summary>
    public static bool AppliesTo(Group? group, decimal? price)
        => group is { Sport: Sport.Futsal or Sport.Poker, EnablePaymentGateways: false }
           && price.GetValueOrDefault() > 0m;

    /// <summary>
    /// C39-A (D3): o service recusa % fora da faixa configurada pelo admin do
    /// sistema — a tela so sugere o minimo.
    /// </summary>
    public bool IsPokerFeePercentInRange(decimal percent)
        => percent >= _feeOptions.Value.PokerFeePercentMin
           && percent <= _feeOptions.Value.PokerFeePercentMax;

    public decimal PokerFeePercentMin => _feeOptions.Value.PokerFeePercentMin;
    public decimal PokerFeePercentMax => _feeOptions.Value.PokerFeePercentMax;

    /// <summary>
    /// Recarimba preco e taxa das inscricoes ainda devidas (sem pagamento e sem
    /// comprovante) com o valor atual do evento, mantendo a isencao vigente na
    /// data de cada confirmacao. Quem ja pagou ou mandou comprovante fica com o
    /// carimbo original. Requer <c>ev.Group</c> e <c>ev.Confirmations</c> carregadas.
    /// </summary>
    public void RestampUnpaid(Event ev)
    {
        foreach (var c in ev.Confirmations.Where(c =>
                     !c.HasPaid
                     && c.PixProofUploadedAt is null
                     && c.PaymentStatus == EventConfirmationPaymentStatus.Pending))
        {
            c.ChargedPrice = ev.Price;
            c.PlatformFeeAmount = ResolveStampForNewConfirmation(ev.Group, ev.Price, c.ConfirmedAt, ev.PlatformFeePercent);
        }
    }

    /// <summary>
    /// Carimbo na criacao da confirmacao (C36-C Fase 0 — ressalva de dinheiro
    /// do C34): a taxa e resolvida UMA vez, aqui. QR, resumo do organizador e
    /// stamp do repasse leem o valor carimbado — a isencao concedida depois
    /// nao retroage, e a expirada depois nao surpreende. Fora do fluxo manual
    /// (gateways ligados ou preco 0) devolve null e o stamp tardio decide.
    /// C39-A: poker resolve % da entrada configurada no evento/mesa; futsal
    /// segue com a taxa fixa configurada.
    /// </summary>
    public decimal? ResolveStampForNewConfirmation(
        Group? group, decimal? price, DateTime nowUtc, decimal? feePercent = null)
    {
        if (!AppliesTo(group, price)) return null;
        if (group is not null && IsWaived(group, nowUtc)) return 0m;

        return group!.Sport == Sport.Poker
            ? Math.Round(price!.Value * (feePercent ?? 0m) / 100m, 2, MidpointRounding.AwayFromZero)
            : ResolveManualFee(group, nowUtc);
    }
}
