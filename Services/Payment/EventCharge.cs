using Confirmai.Models;

namespace Confirmai.Services.Payment;

/// <summary>
/// C39-A: leitura canonica do preco de uma confirmacao. O carimbo
/// (<see cref="EventConfirmation.ChargedPrice"/>) vence — foi o preco
/// anunciado quando o jogador confirmou (e, no cash, a mesa escolhida).
/// Confirmacoes antigas sem carimbo caem para <see cref="Event.Price"/>.
/// Toda leitura de `conf.Event.Price` no caminho do dinheiro passa por aqui
/// (ou pelo equivalente `c.ChargedPrice ?? c.Event.Price` dentro de queries EF).
/// </summary>
public static class EventCharge
{
    public static decimal? PriceOf(EventConfirmation conf)
        => conf.ChargedPrice ?? conf.Event?.Price;

    /// <summary>
    /// Confirmacoes que ocupam vaga, na ordem de chegada. No poker a "lista
    /// de espera" e so visual (posicao > MaxPlayers nao tem direito a pagar).
    /// </summary>
    public static HashSet<int> SlotIds(IEnumerable<EventConfirmation> confirmations, int maxPlayers)
    {
        var ordered = confirmations
            .OrderBy(c => c.ConfirmedAt)
            .ThenBy(c => c.Id)
            .Select(c => c.Id);
        return maxPlayers <= 0
            ? ordered.ToHashSet()
            : ordered.Take(maxPlayers).ToHashSet();
    }

    /// <summary>
    /// True quando a confirmacao esta alem do MaxPlayers do evento.
    /// Requer <c>ev.Confirmations</c> carregada.
    /// </summary>
    public static bool IsWaitlisted(Event ev, EventConfirmation conf)
        => ev.MaxPlayers > 0 && !SlotIds(ev.Confirmations, ev.MaxPlayers).Contains(conf.Id);
}
