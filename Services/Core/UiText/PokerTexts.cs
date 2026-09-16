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

        // Poker detail info component (C28 migration)
        ["Poker.MaxPlayersShort"] = "Máx. jogadores",
        ["Poker.StackMinShort"] = "Stack mínimo",
        ["Poker.StackMaxShort"] = "Stack máximo",

        // Code-behind error messages
        ["Poker.CreateEventError"] = "Erro ao criar evento.",
        ["Poker.AlreadyRegistered"] = "Você já está inscrito.",
        ["Poker.JoinRequestSendError"] = "Não foi possível enviar a solicitação agora. Tente novamente em instantes.",
        ["Poker.JoinRequestCancelError"] = "Não foi possível cancelar a solicitação agora. Tente novamente em instantes.",
    };

    /// <summary>EN-US English (United States) strings</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Page titles
        ["Poker.PageTitle"] = "Poker",
        ["Poker.CreatePageTitle"] = "Create Event · Poker · Confirmai",
        ["Poker.EditPageTitle"] = "Edit Event · Poker · Confirmai",

        // Common
        ["Poker.EventNotFound"] = "Event not found.",
        ["Poker.AccessDenied"] = "Access denied.",
        ["Poker.OnlyOrganizerCanEdit"] = "Only the organizer can edit this event.",
        ["Poker.EventCancelled"] = "This event has been cancelled.",
        ["Poker.CancelEvent"] = "Cancel event",
        ["Poker.CancelConfirm"] = "Are you sure? This action cannot be undone.",
        ["Poker.EditEvent"] = "Edit event",
        ["Poker.BackToList"] = "← Back to list",
        ["Poker.Player"] = "Player",
        ["Poker.You"] = "You",
        ["Poker.Waitlist"] = "waitlist",
        ["Poker.Confirmed"] = "Confirmed",

        // Event types
        ["Poker.Tournament"] = "Tournament",
        ["Poker.CashGame"] = "Cash Game",
        ["Poker.HomeGame"] = "Home Game",
        ["Poker.NewTournament"] = "🏆 New Tournament",
        ["Poker.NewCashGame"] = "💰 New Cash Game",
        ["Poker.NewHomeGame"] = "🏠 New Home Game",
        ["Poker.NewEvent"] = "New Event",
        ["Poker.EditTournament"] = "🏆 Edit Tournament",
        ["Poker.EditCashGame"] = "💰 Edit Cash Game",
        ["Poker.EditHomeGame"] = "🏠 Edit Home Game",
        ["Poker.EditEventDefault"] = "Edit Event",
        ["Poker.CreateEvent"] = "Create Event",

        // Type menu descriptions
        ["Poker.TournamentDesc"] = "Buy-in, rebuy, GTD, late reg…",
        ["Poker.CashGameDesc"] = "Stack min/max, modality, included…",
        ["Poker.HomeGameDesc"] = "Access via private code",

        // My events
        ["Poker.MyMatches"] = "My Matches",

        // Detail page
        ["Poker.OpenGroup"] = "Open group:",
        ["Poker.Inscritos"] = "Registered",
        ["Poker.EnterToParticipate"] = "Join to participate",
        ["Poker.YouAreInscribed"] = "✅ You are registered for this event.",
        ["Poker.CancelInscription"] = "Cancel registration",
        ["Poker.JoinQueue"] = "Join waitlist",
        ["Poker.ConfirmPresence"] = "Confirm attendance",

        // Private group (reuses Futsal keys where identical)
        ["Poker.JoinRequestSent"] = "Request sent — awaiting group admin approval.",
        ["Poker.CancelJoinRequest"] = "Cancel request",
        ["Poker.CancellingJoin"] = "Cancelling…",
        ["Poker.JoinRequestRejected"] = "Your join request was rejected.",
        ["Poker.PrivateGroup"] = "This group is private. Request access from the admin.",
        ["Poker.RequestJoin"] = "Request access",
        ["Poker.SendingJoin"] = "Sending...",

        // Home Game lock
        ["Poker.HomeGameLockText"] = "This Home Game's details are private.\nEnter the access code to see more information.",
        ["Poker.HomeGameEnter"] = "Enter",
        ["Poker.HomeGameCodeWrong"] = "Incorrect code. Check with the organizer.",
        ["Poker.HomeGameDetailsLocked"] = "Details available with access code",
        ["Poker.HomeGameAccess"] = "Access →",
        ["Poker.HomeGameConfirm"] = "Confirm →",

        // Home Game notice (create)
        ["Poker.HomeGameAccessCode"] = "Access code",
        ["Poker.HomeGameNoticeText"] = "A private code will be generated automatically upon creating the event. Share it with your guests — only those with the code will see the home game details.",

        // Home Game edit
        ["Poker.HomeGameCurrentCode"] = "Current code: {0}",
        ["Poker.HomeGameCodeCannotChange"] = "The code cannot be changed — it has already been shared with the guests.",

        // Create/Edit form
        ["Poker.Identity"] = "Identity",
        ["Poker.EventName"] = "Event name",
        ["Poker.PokerHouse"] = "Poker House",
        ["Poker.YourHome"] = "Your home / venue",
        ["Poker.Address"] = "Address",
        ["Poker.AddressPlaceholder"] = "Street, number, neighborhood — City/State",
        ["Poker.City"] = "City",
        ["Poker.CityPlaceholder"] = "São Paulo",
        ["Poker.UF"] = "State",
        ["Poker.SelectUF"] = "— Select —",
        ["Poker.ForeignUF"] = "EX — Foreign",
        ["Poker.DateTime"] = "Date & Time",
        ["Poker.Date"] = "Date",
        ["Poker.StartTime"] = "Start time",
        ["Poker.LateRegDate"] = "Late Registration — date",
        ["Poker.LateRegTime"] = "Late Registration — time",
        ["Poker.LateRegHint"] = "Leave blank if none.",
        ["Poker.LateRegHintEdit"] = "Leave blank to remove.",
        ["Poker.Modality"] = "Modality",
        ["Poker.TournamentStructure"] = "Tournament Structure",
        ["Poker.StartingStack"] = "Starting stack",
        ["Poker.InitialBlindBB"] = "Initial Blind (in BBs)",
        ["Poker.InitialBlindHint"] = "How many chips = 1 BB at the initial level.",
        ["Poker.MaxPlayers"] = "Max. number of players",
        ["Poker.MaxPlayersHint"] = "Use 0 for unlimited.",
        ["Poker.Prices"] = "Prices",
        ["Poker.BuyIn"] = "Buy-in (R$)",
        ["Poker.GTD"] = "GTD — Guaranteed prize (R$)",
        ["Poker.Rebuy"] = "Rebuy (R$)",
        ["Poker.RebuyDouble"] = "Double rebuy (R$)",
        ["Poker.RebuyDoubleHint"] = "May be less than rebuy×2.",
        ["Poker.Addon"] = "Add-on (R$)",
        ["Poker.AddonDouble"] = "Double add-on (R$)",
        ["Poker.Stacks"] = "Stacks",
        ["Poker.StackMin"] = "Min. stack (R$)",
        ["Poker.StackMax"] = "Max. stack (R$)",
        ["Poker.CashIncludes"] = "What is included for players?",
        ["Poker.CashIncludesHint"] = "Optional — e.g. dinner included, drinks, etc.",
        ["Poker.EventForGroup"] = "Event for group",

        // Actions
        ["Poker.CreateButton"] = "Create {0}",
        ["Poker.Creating"] = "Creating…",
        ["Poker.CreatingShort"] = "Creating…",
        ["Poker.SaveChanges"] = "Save changes",
        ["Poker.Saving"] = "Saving…",

        // Late reg display
        ["Poker.LateRegUntil"] = "Late Reg until {0}",

        // Poker detail info component (C28 migration)
        ["Poker.MaxPlayersShort"] = "Max. players",
        ["Poker.StackMinShort"] = "Min. stack",
        ["Poker.StackMaxShort"] = "Max. stack",

        // Code-behind error messages
        ["Poker.CreateEventError"] = "Error creating event.",
        ["Poker.AlreadyRegistered"] = "You are already registered.",
        ["Poker.JoinRequestSendError"] = "Could not send the request now. Please try again in a moment.",
        ["Poker.JoinRequestCancelError"] = "Could not cancel the request now. Please try again in a moment.",
    };

    /// <summary>ES-ES Spanish (Spain) strings</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Page titles
        ["Poker.PageTitle"] = "Poker",
        ["Poker.CreatePageTitle"] = "Crear Evento · Poker · Confirmai",
        ["Poker.EditPageTitle"] = "Editar Evento · Poker · Confirmai",

        // Common
        ["Poker.EventNotFound"] = "Evento no encontrado.",
        ["Poker.AccessDenied"] = "Acceso denegado.",
        ["Poker.OnlyOrganizerCanEdit"] = "Solo el organizador puede editar este evento.",
        ["Poker.EventCancelled"] = "Este evento ha sido cancelado.",
        ["Poker.CancelEvent"] = "Cancelar evento",
        ["Poker.CancelConfirm"] = "¿Está seguro? Esta acción no se puede deshacer.",
        ["Poker.EditEvent"] = "Editar evento",
        ["Poker.BackToList"] = "← Volver a la lista",
        ["Poker.Player"] = "Jugador",
        ["Poker.You"] = "Usted",
        ["Poker.Waitlist"] = "lista de espera",
        ["Poker.Confirmed"] = "Confirmado",

        // Event types
        ["Poker.Tournament"] = "Torneo",
        ["Poker.CashGame"] = "Cash Game",
        ["Poker.HomeGame"] = "Home Game",
        ["Poker.NewTournament"] = "🏆 Nuevo Torneo",
        ["Poker.NewCashGame"] = "💰 Nuevo Cash Game",
        ["Poker.NewHomeGame"] = "🏠 Nuevo Home Game",
        ["Poker.NewEvent"] = "Nuevo Evento",
        ["Poker.EditTournament"] = "🏆 Editar Torneo",
        ["Poker.EditCashGame"] = "💰 Editar Cash Game",
        ["Poker.EditHomeGame"] = "🏠 Editar Home Game",
        ["Poker.EditEventDefault"] = "Editar Evento",
        ["Poker.CreateEvent"] = "Crear Evento",

        // Type menu descriptions
        ["Poker.TournamentDesc"] = "Buy-in, rebuy, GTD, late reg…",
        ["Poker.CashGameDesc"] = "Stack mín/máx, modalidad, incluido…",
        ["Poker.HomeGameDesc"] = "Acceso por código privado",

        // My events
        ["Poker.MyMatches"] = "Mis Partidas",

        // Detail page
        ["Poker.OpenGroup"] = "Abrir grupo:",
        ["Poker.Inscritos"] = "Inscritos",
        ["Poker.EnterToParticipate"] = "Unirse para participar",
        ["Poker.YouAreInscribed"] = "✅ Usted está inscrito en este evento.",
        ["Poker.CancelInscription"] = "Cancelar inscripción",
        ["Poker.JoinQueue"] = "Unirse a la lista de espera",
        ["Poker.ConfirmPresence"] = "Confirmar asistencia",

        // Private group (reuses Futsal keys where identical)
        ["Poker.JoinRequestSent"] = "Solicitud enviada — esperando aprobación del administrador del grupo.",
        ["Poker.CancelJoinRequest"] = "Cancelar solicitud",
        ["Poker.CancellingJoin"] = "Cancelando…",
        ["Poker.JoinRequestRejected"] = "Su solicitud de entrada fue rechazada.",
        ["Poker.PrivateGroup"] = "Este grupo es privado. Solicite entrada al administrador.",
        ["Poker.RequestJoin"] = "Solicitar entrada",
        ["Poker.SendingJoin"] = "Enviando...",

        // Home Game lock
        ["Poker.HomeGameLockText"] = "Los detalles de este Home Game son privados.\nIngrese el código de acceso para ver más información.",
        ["Poker.HomeGameEnter"] = "Entrar",
        ["Poker.HomeGameCodeWrong"] = "Código incorrecto. Verifique con el organizador.",
        ["Poker.HomeGameDetailsLocked"] = "Detalles disponibles con código de acceso",
        ["Poker.HomeGameAccess"] = "Acceder →",
        ["Poker.HomeGameConfirm"] = "Confirmar →",

        // Home Game notice (create)
        ["Poker.HomeGameAccessCode"] = "Código de acceso",
        ["Poker.HomeGameNoticeText"] = "Se generará un código privado automáticamente al crear el evento. Compártalo con sus invitados — solo quienes tengan el código verán los detalles del home game.",

        // Home Game edit
        ["Poker.HomeGameCurrentCode"] = "Código actual: {0}",
        ["Poker.HomeGameCodeCannotChange"] = "El código no se puede cambiar — ya se ha compartido con los invitados.",

        // Create/Edit form
        ["Poker.Identity"] = "Identidad",
        ["Poker.EventName"] = "Nombre del evento",
        ["Poker.PokerHouse"] = "Casa de Poker",
        ["Poker.YourHome"] = "Su casa / local",
        ["Poker.Address"] = "Dirección",
        ["Poker.AddressPlaceholder"] = "Calle, número, barrio — Ciudad/Provincia",
        ["Poker.City"] = "Ciudad",
        ["Poker.CityPlaceholder"] = "São Paulo",
        ["Poker.UF"] = "Provincia",
        ["Poker.SelectUF"] = "— Seleccione —",
        ["Poker.ForeignUF"] = "EX — Extranjero",
        ["Poker.DateTime"] = "Fecha y Hora",
        ["Poker.Date"] = "Fecha",
        ["Poker.StartTime"] = "Hora de inicio",
        ["Poker.LateRegDate"] = "Late Registration — fecha",
        ["Poker.LateRegTime"] = "Late Registration — hora",
        ["Poker.LateRegHint"] = "Dejar en blanco si no hay.",
        ["Poker.LateRegHintEdit"] = "Dejar en blanco para eliminar.",
        ["Poker.Modality"] = "Modalidad",
        ["Poker.TournamentStructure"] = "Estructura del Torneo",
        ["Poker.StartingStack"] = "Stack inicial",
        ["Poker.InitialBlindBB"] = "Blind inicial (en BBs)",
        ["Poker.InitialBlindHint"] = "Cuántas fichas = 1 BB en el nivel inicial.",
        ["Poker.MaxPlayers"] = "Nº máximo de jugadores",
        ["Poker.MaxPlayersHint"] = "Use 0 para ilimitado.",
        ["Poker.Prices"] = "Precios",
        ["Poker.BuyIn"] = "Buy-in (R$)",
        ["Poker.GTD"] = "GTD — Premio garantizado (R$)",
        ["Poker.Rebuy"] = "Rebuy (R$)",
        ["Poker.RebuyDouble"] = "Rebuy doble (R$)",
        ["Poker.RebuyDoubleHint"] = "Puede ser menor que rebuy×2.",
        ["Poker.Addon"] = "Add-on (R$)",
        ["Poker.AddonDouble"] = "Add-on doble (R$)",
        ["Poker.Stacks"] = "Stacks",
        ["Poker.StackMin"] = "Stack mínimo (R$)",
        ["Poker.StackMax"] = "Stack máximo (R$)",
        ["Poker.CashIncludes"] = "¿Qué está incluido para los jugadores?",
        ["Poker.CashIncludesHint"] = "Opcional — ej: cena incluida, bebidas, etc.",
        ["Poker.EventForGroup"] = "Evento para el grupo",

        // Actions
        ["Poker.CreateButton"] = "Crear {0}",
        ["Poker.Creating"] = "Creando…",
        ["Poker.CreatingShort"] = "Creando…",
        ["Poker.SaveChanges"] = "Guardar cambios",
        ["Poker.Saving"] = "Guardando…",

        // Late reg display
        ["Poker.LateRegUntil"] = "Late Reg hasta {0}",

        // Poker detail info component (C28 migration)
        ["Poker.MaxPlayersShort"] = "Máx. jugadores",
        ["Poker.StackMinShort"] = "Stack mínimo",
        ["Poker.StackMaxShort"] = "Stack máximo",

        // Code-behind error messages
        ["Poker.CreateEventError"] = "Error al crear el evento.",
        ["Poker.AlreadyRegistered"] = "Ya está inscrito.",
        ["Poker.JoinRequestSendError"] = "No se pudo enviar la solicitud ahora. Inténtelo de nuevo en unos instantes.",
        ["Poker.JoinRequestCancelError"] = "No se pudo cancelar la solicitud ahora. Inténtelo de nuevo en unos instantes.",
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
