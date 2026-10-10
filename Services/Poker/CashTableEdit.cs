using Confirmai.Models;
using Confirmai.Services.Payment;

namespace Confirmai.Services.Poker;

/// <summary>C39-B F10: linha de mesa vinda do formulario de edicao.</summary>
public sealed record CashTableFormRow(
    int? Id, string Label, decimal Price, decimal FeePercent, bool IsActive);

public sealed record CashTableEditPlan(
    List<EventPriceOption> Added,
    List<EventPriceOption> Deactivated,
    List<EventPriceOption> Deleted,
    HashSet<int> RestampedOptionIds);

/// <summary>
/// C39-B F10: diff das mesas do cash na edicao — regra pura e testavel.
/// Mesa com pagante/comprovante tem preco e % congelados (rotulo e
/// desativacao seguem livres); mesa com inscrito nunca e apagada, so
/// desativada. <see cref="Apply"/> roda depois do <see cref="Validate"/> e
/// das guardas de Pix da pagina, e devolve as mesas cujo preco/% mudaram
/// para o caller recarimbar quem ainda deve.
/// </summary>
public static class CashTableEdit
{
    /// <summary>Chave i18n do primeiro erro, ou null quando o form e valido.</summary>
    public static string? Validate(
        IReadOnlyList<CashTableFormRow> rows,
        IReadOnlyDictionary<int, EventPriceOption> existingById,
        ISet<int> lockedOptionIds,
        PlatformFeePolicy feePolicy)
    {
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Label))
                return "Poker.Create.TableLabelRequired";
            if (r.Price < 0m)
                return "Poker.Create.TablePriceInvalid";
            if (r.Price > 0m && !feePolicy.IsPokerFeePercentInRange(r.FeePercent))
                return "Poker.Create.FeePercentOutOfRange";
            if (r.Id is int id
                && lockedOptionIds.Contains(id)
                && existingById.TryGetValue(id, out var existing)
                && (existing.Price != r.Price || existing.PlatformFeePercent != r.FeePercent))
                return "Poker.Edit.PriceLockedPaid";
        }
        return null;
    }

    /// <summary>True quando o form deixa alguma mesa ativa com preco > 0.</summary>
    public static bool WillCharge(IEnumerable<CashTableFormRow> rows)
        => rows.Any(r => r.IsActive && r.Price > 0m);

    /// <summary>
    /// Aplica o diff sobre <c>ev.PriceOptions</c> (entidades rastreadas):
    /// atualiza existentes, adiciona novas, desativa removidas com inscritos
    /// e apaga removidas sem ninguem. Linhas com <c>Id</c> que nao existem
    /// no evento sao tratadas como novas (defesa contra form adulterado).
    /// </summary>
    public static CashTableEditPlan Apply(
        Event ev, IReadOnlyList<CashTableFormRow> rows, ISet<int> usedOptionIds)
    {
        var plan = new CashTableEditPlan(
            new List<EventPriceOption>(), new List<EventPriceOption>(),
            new List<EventPriceOption>(), new HashSet<int>());

        var seen = new HashSet<int>();
        var sort = 0;
        foreach (var r in rows)
        {
            if (r.Id is int id
                && ev.PriceOptions.FirstOrDefault(o => o.Id == id) is { } opt)
            {
                seen.Add(id);
                if (opt.Price != r.Price || opt.PlatformFeePercent != r.FeePercent)
                    plan.RestampedOptionIds.Add(id);
                opt.Label = r.Label.Trim();
                opt.Price = r.Price;
                opt.PlatformFeePercent = r.FeePercent;
                opt.IsActive = r.IsActive;
                opt.SortOrder = sort++;
            }
            else
            {
                var added = new EventPriceOption
                {
                    Label = r.Label.Trim(),
                    Price = r.Price,
                    PlatformFeePercent = r.FeePercent,
                    IsActive = r.IsActive,
                    SortOrder = sort++,
                };
                ev.PriceOptions.Add(added);
                plan.Added.Add(added);
            }
        }

        foreach (var opt in ev.PriceOptions.Where(o => o.Id != 0 && !seen.Contains(o.Id)).ToList())
        {
            if (usedOptionIds.Contains(opt.Id))
            {
                if (opt.IsActive)
                {
                    opt.IsActive = false;
                    plan.Deactivated.Add(opt);
                }
            }
            else
            {
                ev.PriceOptions.Remove(opt);
                plan.Deleted.Add(opt);
            }
        }

        return plan;
    }
}
