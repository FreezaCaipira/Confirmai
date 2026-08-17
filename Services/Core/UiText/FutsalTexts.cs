namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Futsal UI text domain: match detail, create, edit, escalacao, schedule, components.
/// </summary>
internal static class FutsalTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Page titles
        ["Futsal.PageTitle"] = "Partida",
        ["Futsal.CreatePageTitle"] = "Criar Partida · Futsal · Confirmai",
        ["Futsal.EditPageTitle"] = "Editar Partida · Futsal · Confirmai",
        ["Futsal.ScheduleEditPageTitle"] = "Editar Partida Semanal · Confirmai",
        ["Futsal.ScheduleIndexPageTitle"] = "Partidas Semanais — Confirmai",
        ["Futsal.EscalacaoPageTitle"] = "Escalação",
        ["Futsal.PostMatchPageTitle"] = "Pós-Partida",

        // Common match strings
        ["Futsal.MatchNotFound"] = "Partida não encontrada.",
        ["Futsal.ScheduleNotFound"] = "Agendamento não encontrado.",
        ["Futsal.AccessDenied"] = "Acesso negado.",
        ["Futsal.OnlyOrganizerCanEdit"] = "Apenas o organizador pode editar esta partida.",
        ["Futsal.OnlyCreatorCanEditSchedule"] = "Apenas o criador pode editar este agendamento.",
        ["Futsal.MatchCancelled"] = "Esta partida foi cancelada.",
        ["Futsal.EnterToConfirm"] = "Entrar para confirmar presença",
        ["Futsal.ConfirmPresence"] = "Confirmar presença",
        ["Futsal.NewMatch"] = "Nova Partida",
        ["Futsal.NewMatchFutsal"] = "Nova Partida de Futsal",
        ["Futsal.NewMatchGroup"] = "Nova Partida — {0}",
        ["Futsal.CreateMatch"] = "Criar Partida",
        ["Futsal.Creating"] = "Criando…",
        ["Futsal.EditMatch"] = "Editar Partida",
        ["Futsal.SaveChanges"] = "Salvar Alterações",
        ["Futsal.Saving"] = "Salvando…",

        // Player tags
        ["Futsal.Player"] = "Jogador",
        ["Futsal.Goalkeeper"] = "Goleiro",
        ["Futsal.You"] = "Você",
        ["Futsal.Mvp"] = "⭐ Destaque",
        ["Futsal.NoPay"] = "não paga",
        ["Futsal.Paid"] = "pago",
        ["Futsal.Pending"] = "pendente",
        ["Futsal.Pay"] = "pagar",
        ["Futsal.MarkPaid"] = "marcar pago?",
        ["Futsal.UnmarkPaid"] = "desmarcar?",
        ["Futsal.Proof"] = "📎 comprovante",
        ["Futsal.ProofTitle"] = "Comprovante enviado - clique para gerenciar",
        ["Futsal.ProofSent"] = "Comprovante enviado",
        ["Futsal.NoProofTitle"] = "Sem comprovante - clique para marcar pago",
        ["Futsal.GatewayPaidTitle"] = "Pago via gateway — não pode ser desfeito pelo admin",
        ["Futsal.Receiver"] = "recebedor",

        // Actions
        ["Futsal.Remove"] = "remover",
        ["Futsal.RemoveConfirm"] = "remover?",
        ["Futsal.Cancel"] = "cancelar",
        ["Futsal.CancelSignupConfirm"] = "cancelar inscrição?",
        ["Futsal.Waitlist"] = "Entrar na lista de espera",
        ["Futsal.WaitlistGk"] = "Entrar na lista de espera de goleiros",
        ["Futsal.WaitlistLine"] = "Entrar na lista de espera de linha",
        ["Futsal.RemoveSlot"] = "Remover vaga",
        ["Futsal.AddSlot"] = "Adicionar vaga",
        ["Futsal.RemoveSlotGk"] = "Remover vaga de goleiro",
        ["Futsal.AddSlotGk"] = "Adicionar vaga de goleiro",
        ["Futsal.RemoveSlotLine"] = "Remover vaga de linha",
        ["Futsal.AddSlotLine"] = "Adicionar vaga de linha",

        // Confirmed banner
        ["Futsal.ConfirmedToPlay"] = "Confirmado para jogar na",
        ["Futsal.Gol"] = "GOL",
        ["Futsal.Linha"] = "LINHA",
        ["Futsal.PaidAmount"] = "Pago R$ {0}",
        ["Futsal.PayAmount"] = "Pagar R$ {0}",

        // Private group
        ["Futsal.JoinRequestSent"] = "Solicitação enviada — aguardando aprovação do administrador do grupo.",
        ["Futsal.CancelJoinRequest"] = "Cancelar solicitação",
        ["Futsal.CancellingJoin"] = "Cancelando…",
        ["Futsal.JoinRequestRejected"] = "Sua solicitação de entrada foi recusada.",
        ["Futsal.PrivateGroup"] = "Este grupo é privado. Solicite entrada para o administrador.",
        ["Futsal.RequestJoin"] = "Solicitar entrada",
        ["Futsal.SendingJoin"] = "Enviando...",

        // Escalacao
        ["Futsal.EscalacaoNotAvailable"] = "Escalação não disponível",
        ["Futsal.EscalacaoNotMounted"] = "A escalação ainda não foi montada pelo organizador.",
        ["Futsal.BackToMatch"] = "Voltar à partida",
        ["Futsal.PostMatchOnlyMembers"] = "Apenas membros do grupo podem ver o resumo e votar no destaque.",
        ["Futsal.MontarEscalacao"] = "Montar Escalação",
        ["Futsal.VerEscalacao"] = "Ver Escalação",
        ["Futsal.EscalacaoConfirmedAt"] = "Escalação confirmada em {0}",
        ["Futsal.ResetEscalacao"] = "Resetar escalação",
        ["Futsal.ResetConfirm"] = "Tem certeza? A escalação será desfeita e os jogadores voltarão a não ter time atribuído.",
        ["Futsal.Resetting"] = "Resetando…",
        ["Futsal.SimResetar"] = "Sim, resetar",
        ["Futsal.ShufflePlayers"] = "Embaralhar jogadores de linha",
        ["Futsal.ConfirmEscalacao"] = "Confirmar escalação",
        ["Futsal.TeamsUnbalanced"] = "Times desiguais — iguale os jogadores de linha para confirmar.",
        ["Futsal.ConfirmEscalacaoModal"] = "Confirmar esta escalação? Os times ficarão visíveis para todos os confirmados.",
        ["Futsal.SimConfirmar"] = "Sim, confirmar",

        // Escalacao share
        ["Futsal.Copied"] = "Copiado!",
        ["Futsal.CopyEscalacao"] = "Copiar escalação",
        ["Futsal.ShareWhatsApp"] = "Compartilhar no WhatsApp",

        // Escalacao score
        ["Futsal.Score"] = "Placar",
        ["Futsal.TeamWon"] = "🏆 {0} venceu!",
        ["Futsal.Draw"] = "🤝 Empate",
        ["Futsal.RegisteredBy"] = "Registrado por {0}",
        ["Futsal.RegisteredAt"] = "em {0}",
        ["Futsal.ConfirmScore"] = "Confirmar placar",
        ["Futsal.ScoreNotRegistered"] = "Placar ainda não registrado.",

        // Quorum
        ["Futsal.MatchEnded"] = "Partida encerrada",
        ["Futsal.QuorumReached"] = "Quantidade mínima de jogadores atingida",
        ["Futsal.QuorumNeeded"] = "Quantidade mínima de jogadores é: {0} de linha + {1} goleiro(s)",
        ["Futsal.LackingPlayers"] = "Faltam {0} jogador(es) para atingir o mínimo necessário",

        // Location
        ["Futsal.Location"] = "Localização",
        ["Futsal.LocationIframeTitle"] = "Localização da quadra",
        ["Futsal.OpenInMaps"] = "Abrir no Google Maps",

        // Form fields
        ["Futsal.Identity"] = "Identidade da Partida",
        ["Futsal.MatchName"] = "Nome da partida",
        ["Futsal.MatchNamePlaceholder"] = "Ex: \"Rachão da Galera do Zé\", \"Pelada de Quinta\"",
        ["Futsal.LocalNameLabel"] = "Como vocês chamam o jogo na sua região?",
        ["Futsal.LocalNamePlaceholder"] = "racha, pelada, baba, rachão…",
        ["Futsal.LocalNameHint"] = "Opcional — aparece nos cards para identificar o estilo da região.",
        ["Futsal.Modality"] = "Modalidade",
        ["Futsal.ModalityFutsal"] = "Futsal",
        ["Futsal.ModalitySociety"] = "Fut7 / Society",
        ["Futsal.ModalityCampo"] = "Campo",
        ["Futsal.Venue"] = "Local",
        ["Futsal.VenueField"] = "Quadra / Society / Campo",
        ["Futsal.VenueSelect"] = "— Selecione a quadra —",
        ["Futsal.VenueSelectShort"] = "— selecione —",
        ["Futsal.NoVenueRegistered"] = "Nenhuma quadra cadastrada ainda. Solicite ao administrador do sistema.",
        ["Futsal.DateTime"] = "Data & Horário",
        ["Futsal.Date"] = "Data",
        ["Futsal.StartTime"] = "Hora de início",
        ["Futsal.Duration"] = "Duração",
        ["Futsal.Slots"] = "Vagas",
        ["Futsal.TotalSlots"] = "Vagas totais (linha + goleiros)",
        ["Futsal.Goalkeepers"] = "Goleiros",
        ["Futsal.NoGkHint"] = "0 = sem reserva de goleiro.",
        ["Futsal.RotateInGoal"] = "Revezar jogadores de linha no gol",
        ["Futsal.Price"] = "Valor",
        ["Futsal.PricePerPlayer"] = "Valor por jogador (R$)",
        ["Futsal.PriceHint"] = "Use 0 para partidas sem cobrança.",
        ["Futsal.CancelMatch"] = "Cancelar partida",
        ["Futsal.CancelMatchConfirm"] = "Tem certeza? Todos os confirmados serão avisados.",
        ["Futsal.No"] = "Não",
        ["Futsal.SimCancelar"] = "Sim, cancelar",

        // Schedule
        ["Futsal.WeeklyMatches"] = "Partidas Semanais",
        ["Futsal.LoadingWeekly"] = "Carregando suas partidas semanais…",
        ["Futsal.NoWeeklyMatches"] = "Nenhuma partida semanal cadastrada.",
        ["Futsal.NoWeeklyHint"] = "Crie uma partida semanal para gerar partidas automaticamente toda semana.",
        ["Futsal.Inactive"] = "Inativa",
        ["Futsal.Players"] = "jogadores",
        ["Futsal.GoalkeepersShort"] = "goleiros",
        ["Futsal.Next"] = "Próxima: {0}",
        ["Futsal.ConfirmedCount"] = "confirmados",
        ["Futsal.NoFutureMatch"] = "Nenhuma partida futura gerada.",
        ["Futsal.Deactivate"] = "Desativar",
        ["Futsal.Reactivate"] = "Reativar",
        ["Futsal.ScheduleWarning"] = "⚠️ Alterações aqui não afetam partidas já geradas — apenas as próximas ocorrências.",
        ["Futsal.Identification"] = "Identificação",
        ["Futsal.Schedule"] = "Horário",
        ["Futsal.DayOfWeek"] = "Dia da semana",
        ["Futsal.Create.PixRequired"] = "Configure sua chave Pix no perfil antes de criar uma partida com preço.",

        // Futsal components (C28 migration)
        ["Futsal.WaitlistTitle"] = "Lista de espera",
        ["Futsal.Reserve"] = "Reserva",
        ["Futsal.MatchHighlight"] = "Destaque da Partida",
        ["Futsal.DaysOfWeek"] = "Dias da semana",
    };

    /// <summary>EN-US English (United States) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Futsal components (C28 migration)
        ["Futsal.WaitlistTitle"] = "Waitlist",
        ["Futsal.Reserve"] = "Reserve",
        ["Futsal.MatchHighlight"] = "Match Highlight",
        ["Futsal.DaysOfWeek"] = "Days of the week",
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Futsal components (C28 migration)
        ["Futsal.WaitlistTitle"] = "Lista de espera",
        ["Futsal.Reserve"] = "Reserva",
        ["Futsal.MatchHighlight"] = "Destaque de la Partida",
        ["Futsal.DaysOfWeek"] = "Días de la semana",
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
