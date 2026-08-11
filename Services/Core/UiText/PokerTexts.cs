namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Poker UI text domain: tournament, cash game, home game detail/create/edit/index.
/// </summary>
internal static class PokerTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Page titles
        ["Poker.PageTitle"] = "Poker",
        ["Poker.CreatePageTitle"] = "Criar Evento · Poker · Confirmai",
        ["Poker.EditPageTitle"] = "Editar Evento · Poker · Confirmai",

        // Common
        ["Poker.EventNotFound"] = "Evento não encontrado.",
        ["Poker.AccessDenied"] = "Acesso negado.",
        ["Poker.OnlyOrganizerCanEdit"] = "Apenas o organizador pode editar este evento.",
        ["Poker.EventCancelled"] = "Este evento foi cancelado.",
        ["Poker.CancelEvent"] = "Cancelar evento",
        ["Poker.CancelConfirm"] = "Tem certeza? Esta ação não pode ser desfeita.",
        ["Poker.EditEvent"] = "Editar evento",
        ["Poker.BackToList"] = "← Voltar à lista",
        ["Poker.Player"] = "Jogador",
        ["Poker.You"] = "Você",
        ["Poker.Waitlist"] = "fila",
        ["Poker.Confirmed"] = "Confirmado",

        // Event types
        ["Poker.Tournament"] = "Torneio",
        ["Poker.CashGame"] = "Cash Game",
        ["Poker.HomeGame"] = "Home Game",
        ["Poker.NewTournament"] = "🏆 Novo Torneio",
        ["Poker.NewCashGame"] = "💰 Novo Cash Game",
        ["Poker.NewHomeGame"] = "🏠 Novo Home Game",
        ["Poker.NewEvent"] = "Novo Evento",
        ["Poker.EditTournament"] = "🏆 Editar Torneio",
        ["Poker.EditCashGame"] = "💰 Editar Cash Game",
        ["Poker.EditHomeGame"] = "🏠 Editar Home Game",
        ["Poker.EditEventDefault"] = "Editar Evento",
        ["Poker.CreateEvent"] = "Criar Evento",

        // Type menu descriptions
        ["Poker.TournamentDesc"] = "Buy-in, rebuy, GTD, late reg…",
        ["Poker.CashGameDesc"] = "Stack mín/máx, modalidade, incluso…",
        ["Poker.HomeGameDesc"] = "Acesso por código privado",

        // My events
        ["Poker.MyMatches"] = "Minhas Partidas",

        // Detail page
        ["Poker.OpenGroup"] = "Abrir grupo:",
        ["Poker.Inscritos"] = "Inscritos",
        ["Poker.EnterToParticipate"] = "Entrar para participar",
        ["Poker.YouAreInscribed"] = "✅ Você está inscrito neste evento.",
        ["Poker.CancelInscription"] = "Cancelar inscrição",
        ["Poker.JoinQueue"] = "Entrar na fila",
        ["Poker.ConfirmPresence"] = "Confirmar presença",

        // Private group (reuses Futsal keys where identical)
        ["Poker.JoinRequestSent"] = "Solicitação enviada — aguardando aprovação do administrador do grupo.",
        ["Poker.CancelJoinRequest"] = "Cancelar solicitação",
        ["Poker.CancellingJoin"] = "Cancelando…",
        ["Poker.JoinRequestRejected"] = "Sua solicitação de entrada foi recusada.",
        ["Poker.PrivateGroup"] = "Este grupo é privado. Solicite entrada para o administrador.",
        ["Poker.RequestJoin"] = "Solicitar entrada",
        ["Poker.SendingJoin"] = "Enviando...",

        // Home Game lock
        ["Poker.HomeGameLockText"] = "Os detalhes deste Home Game são privados.\nDigite o código de acesso para ver mais informações.",
        ["Poker.HomeGameEnter"] = "Entrar",
        ["Poker.HomeGameCodeWrong"] = "Código incorreto. Verifique com o organizador.",
        ["Poker.HomeGameDetailsLocked"] = "Detalhes disponíveis com código de acesso",
        ["Poker.HomeGameAccess"] = "Acessar →",
        ["Poker.HomeGameConfirm"] = "Confirmar →",

        // Home Game notice (create)
        ["Poker.HomeGameAccessCode"] = "Código de acesso",
        ["Poker.HomeGameNoticeText"] = "Um código privado será gerado automaticamente ao criar o evento. Compartilhe com seus convidados — apenas quem tiver o código verá os detalhes do home game.",

        // Home Game edit
        ["Poker.HomeGameCurrentCode"] = "Código atual: {0}",
        ["Poker.HomeGameCodeCannotChange"] = "O código não pode ser alterado — já foi compartilhado com os convidados.",

        // Create/Edit form
        ["Poker.Identity"] = "Identidade",
        ["Poker.EventName"] = "Nome do evento",
        ["Poker.PokerHouse"] = "Casa de Poker",
        ["Poker.YourHome"] = "Sua casa / local",
        ["Poker.Address"] = "Endereço",
        ["Poker.AddressPlaceholder"] = "Rua, número, bairro — Cidade/UF",
        ["Poker.City"] = "Cidade",
        ["Poker.CityPlaceholder"] = "São Paulo",
        ["Poker.UF"] = "UF",
        ["Poker.SelectUF"] = "— Selecione —",
        ["Poker.ForeignUF"] = "EX — Estrangeiro",
        ["Poker.DateTime"] = "Data & Horário",
        ["Poker.Date"] = "Data",
        ["Poker.StartTime"] = "Hora de início",
        ["Poker.LateRegDate"] = "Late Registration — data",
        ["Poker.LateRegTime"] = "Late Registration — hora",
        ["Poker.LateRegHint"] = "Deixe em branco se não houver.",
        ["Poker.LateRegHintEdit"] = "Deixe em branco para remover.",
        ["Poker.Modality"] = "Modalidade",
        ["Poker.TournamentStructure"] = "Estrutura do Torneio",
        ["Poker.StartingStack"] = "Fichas iniciais",
        ["Poker.InitialBlindBB"] = "Blind inicial (em BBs)",
        ["Poker.InitialBlindHint"] = "Quantas fichas = 1 BB no nível inicial.",
        ["Poker.MaxPlayers"] = "Nº máximo de jogadores",
        ["Poker.MaxPlayersHint"] = "Use 0 para ilimitado.",
        ["Poker.Prices"] = "Preços",
        ["Poker.BuyIn"] = "Buy-in (R$)",
        ["Poker.GTD"] = "GTD — Premiação garantida (R$)",
        ["Poker.Rebuy"] = "Rebuy (R$)",
        ["Poker.RebuyDouble"] = "Rebuy duplo (R$)",
        ["Poker.RebuyDoubleHint"] = "Pode ser menor que rebuy×2.",
        ["Poker.Addon"] = "Add-on (R$)",
        ["Poker.AddonDouble"] = "Add-on duplo (R$)",
        ["Poker.Stacks"] = "Stacks",
        ["Poker.StackMin"] = "Stack mínimo (R$)",
        ["Poker.StackMax"] = "Stack máximo (R$)",
        ["Poker.CashIncludes"] = "O que está incluso para os jogadores?",
        ["Poker.CashIncludesHint"] = "Opcional — ex: janta inclusa, bebidas, etc.",
        ["Poker.EventForGroup"] = "Evento para o grupo",

        // Actions
        ["Poker.CreateButton"] = "Criar {0}",
        ["Poker.Creating"] = "Criando…",
        ["Poker.CreatingShort"] = "Criando…",
        ["Poker.SaveChanges"] = "Salvar alterações",
        ["Poker.Saving"] = "Salvando…",

        // Late reg display
        ["Poker.LateRegUntil"] = "Late Reg até {0}",
    };

    /// <summary>EN-US English (United States) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>Get combined dictionary for all languages</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> GetAllTexts()
    {
        return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pt-BR"] = PtBr,
            ["en-US"] = EnUs,
            ["es-ES"] = EsEs,
        };
    }
}
