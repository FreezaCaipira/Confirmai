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
    /// Preco cobrado pelo app para um torneio com o buy-in informado. Torneio
    /// anterior ao C39 (buy-in anunciado e <see cref="Event.Price"/> nulo) segue
    /// pago na mesa: editar a partida nao liga a cobranca de quem ja se inscreveu.
    /// </summary>
    public static decimal? TournamentPrice(Event ev, decimal buyIn)
        => buyIn <= 0 || (ev.Price is null && ev.BuyInAmount.GetValueOrDefault() > 0)
            ? null
            : buyIn;

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

    /// <summary>
    /// Ids das confirmacoes em lista de espera, dado um lote que contem TODAS
    /// as confirmacoes dos eventos de poker em questao (nao so o subconjunto
    /// filtrado pelo caller). Usado por inadimplencia/saida-com-divida para
    /// ignorar quem esta na espera — quem nao tem vaga nao deve (F5).
    /// </summary>
    public static HashSet<int> WaitlistedIds(
        IEnumerable<(int Id, int EventId, DateTime ConfirmedAt)> confirmations,
        IReadOnlyDictionary<int, int> maxPlayersByEvent)
    {
        var set = new HashSet<int>();
        foreach (var g in confirmations.GroupBy(c => c.EventId))
        {
            if (!maxPlayersByEvent.TryGetValue(g.Key, out var max) || max <= 0) continue;
            foreach (var c in g.OrderBy(c => c.ConfirmedAt).ThenBy(c => c.Id).Skip(max))
                set.Add(c.Id);
        }
        return set;
    }
}
