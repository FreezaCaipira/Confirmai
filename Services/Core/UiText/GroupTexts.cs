namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Group UI text domain: group detail, create, join, features, payments, ranking, partidas, components.
/// </summary>
internal static class GroupTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Page titles
        ["Group.PageTitle"] = "Grupo",
        ["Group.CreatePageTitle"] = "Criar Grupo — Confirmai",
        ["Group.InvitePageTitle"] = "Convite",
        ["Group.ConfigPageTitle"] = "Configurações — {0}",
        ["Group.PaymentsPageTitle"] = "Pagamentos — {0}",
        ["Group.RankingPageTitle"] = "Ranking — {0}",
        ["Group.PartidasPageTitle"] = "Partidas — {0}",

        // Common
        ["Group.NotFound"] = "Grupo não encontrado.",
        ["Group.NotAuthorized"] = "Acesso não autorizado.",
        ["Group.FeatureNotEnabled"] = "Funcionalidade não habilitada.",
        ["Group.Inactive"] = "Grupo inativo.",
        ["Group.BackToGroup"] = "← Voltar ao grupo",
        ["Group.BackToStart"] = "← Voltar ao início",
        ["Group.Member"] = "Membro",
        ["Group.You"] = "Você",
        ["Group.Admin"] = "admin",
        ["Group.Creator"] = "criador",
        ["Group.InactiveTag"] = "inativo",
        ["Group.SportFutsal"] = "⚽ Futsal",
        ["Group.SportPoker"] = "🃏 Poker",

        // Index page
        ["Group.JoinCodeHint"] = "Use um código de convite ou crie um novo grupo para começar.",
        ["Group.Approve"] = "Aprovar",
        ["Group.PendingSingular"] = "pendente",
        ["Group.PendingPlural"] = "pendentes",
        ["Group.PixPending"] = "⚠ Pix pendente",
        ["Group.PixPendingTitle"] = "Nenhum admin tem chave Pix cadastrada",
        ["Group.NoMatchScheduled"] = "Sem partida agendada",
        ["Group.MembersSingular"] = "membro",
        ["Group.MembersPlural"] = "membros",
        ["Group.Weekly"] = "Semanal",
        ["Group.WeeklyTitle"] = "Partida semanal automática",

        // Detail page
        ["Group.InviteLink"] = "Link de convite:",
        ["Group.Code"] = "Código:",
        ["Group.Copy"] = "Copiar",
        ["Group.Copied"] = "Copiado!",
        ["Group.Invite"] = "Convidar",
        ["Group.Settings"] = "Configurações",
        ["Group.Metrics"] = "Métricas",
        ["Group.Members"] = "Membros",
        ["Group.PartidasLink"] = "Partidas",
        ["Group.RankingLink"] = "Ranking",
        ["Group.PaymentsLink"] = "Pagamentos",
        ["Group.PixNotConfigured"] = "Pix do organizador não configurado",
        ["Group.PixNotConfiguredDesc"] = "Nenhum administrador do grupo possui chave Pix cadastrada. Os participantes não terão opção de pagamento manual por Pix.",
        ["Group.ConfigureNow"] = "Configurar agora →",
        ["Group.JoinRequestSent"] = "Solicitação enviada — aguardando aprovação do administrador.",
        ["Group.CancelJoinRequest"] = "Cancelar solicitação",
        ["Group.CancellingJoin"] = "Cancelando…",
        ["Group.JoinRequestRejected"] = "Sua solicitação foi recusada.",
        ["Group.NotMemberInfo"] = "Você não é membro deste grupo. Solicite entrada ou peça o código de convite ao organizador.",
        ["Group.RequestJoin"] = "Solicitar entrada",
        ["Group.SendingJoin"] = "Enviando…",

        // Create page
        ["Group.NewGroup"] = "Novo Grupo",
        ["Group.CreateInfoBox"] = "Crie um grupo e convide jogadores para participarem das partidas!",
        ["Group.GroupIdentity"] = "Identidade do Grupo",
        ["Group.Sport"] = "Esporte",
        ["Group.SportFutsalOption"] = "Futsal / Futebol",
        ["Group.SportPokerOption"] = "Poker",
        ["Group.GroupName"] = "Nome do grupo",
        ["Group.GroupLogo"] = "Logo/Imagem do grupo",
        ["Group.NoImageSelected"] = "Nenhuma imagem selecionada",
        ["Group.SelectImage"] = "Selecionar imagem",
        ["Group.UploadHint"] = "PNG, JPG ou WebP • Máximo 5 MB",
        ["Group.Location"] = "Localização",
        ["Group.SelectUF"] = "— Selecione —",
        ["Group.ForeignUF"] = "EX — Estrangeiro",
        ["Group.SelectCity"] = "Selecione a UF",
        ["Group.CreateButton"] = "Criar Grupo",
        ["Group.Creating"] = "Criando…",

        // Join page
        ["Group.InvalidInvite"] = "Convite inválido ou expirado.",
        ["Group.Home"] = "Início",
        ["Group.EnterToJoin"] = "Entrar para participar do grupo",
        ["Group.AlreadyMember"] = "Você já é membro deste grupo.",
        ["Group.ViewGroup"] = "Ver grupo",
        ["Group.JoinedGroup"] = "Você entrou no grupo {0}!",
        ["Group.JoinGroupBtn"] = "Entrar no grupo",
        ["Group.Cancelled"] = "cancelado",
        ["Group.Full"] = "lotado",
        ["Group.Open"] = "aberto",
        ["Group.DateTimeHeader"] = "Data/Hora",
        ["Group.PlayersHeader"] = "Jogadores",
        ["Group.StatusHeader"] = "Status",

        // Features page
        ["Group.ConfigTitle"] = "Configurações do grupo",

        // FeaturesToggles component
        ["Group.PaymentsSection"] = "Pagamentos",
        ["Group.PaymentsSectionSub"] = "Escolha como os jogadores pagam as partidas deste grupo.",
        ["Group.GatewayTitle"] = "Intermédio do site (Gateways de pagamento)",
        ["Group.GatewayDesc"] = "Quando ativado, o pagamento das partidas é processado via gateways (EfiBank, Abacate e futuras integrações) com repasse automático ao organizador. Desativar significa pagamento via Pix direto ao organizador, mas funcionalidades adicionais como ranking e votação do melhor da partida ficam indisponíveis.",
        ["Group.NoGateway"] = "Nenhum gateway está disponível no momento. Habilite/configure em Admin > Gateways.",
        ["Group.AvailableNow"] = "Disponíveis agora: {0}",
        ["Group.Activate"] = "Ativar",
        ["Group.Deactivate"] = "Desativar",
        ["Group.AdditionalFeatures"] = "Funcionalidades adicionais",
        ["Group.AdditionalFeaturesSub"] = "Ative ou desative funcionalidades opcionais para este grupo.",
        ["Group.RankingPostMatch"] = "Ranking pós-partida",
        ["Group.RankingPostMatchDesc"] = "Exibe um ranking de participações e desempenho dos membros, com visões mensal e anual, na página do grupo.",
        ["Group.RequiresGateways"] = "Requer intermédio do site (gateways) ativado.",
        ["Group.BestPlayerVoting"] = "Votação melhor da partida",
        ["Group.BestPlayerVotingDesc"] = "Permite que os membros votem no melhor jogador da partida após o encerramento do evento.",

        // MembersManager component
        ["Group.MembersPermissions"] = "Membros & Permissões",
        ["Group.MembersPermissionsSub"] = "Promova membros a admin ou remova permissões de admin.",
        ["Group.OnlyCreatorCanManage"] = "Apenas o criador do grupo pode adicionar ou remover administradores.",
        ["Group.RemoveAdmin"] = "Remover admin",
        ["Group.MakeAdmin"] = "Tornar admin",
        ["Group.MakeAdminConfirm"] = "tornar admin?",
        ["Group.PromoteToAdmin"] = "Promover a admin",
        ["Group.MustHaveOneAdmin"] = "O grupo deve ter pelo menos um admin",
        ["Group.RemoveAdminTitle"] = "Remover permissão de admin",

        // PayoutAccountEditor component
        ["Group.PayoutTitle"] = "Chave PIX de Repasse (Taxa de Serviço)",
        ["Group.PayoutSub"] = "Configure a chave PIX do organizador para receber o valor das partidas (após dedução da taxa de serviço). Esta chave será usada para envio automático via API quando pagamentos são confirmados.",
        ["Group.PayoutRegistered"] = "Chave cadastrada:",
        ["Group.PayoutBeneficiary"] = "Beneficiário: {0}",
        ["Group.PixKeyType"] = "Tipo de chave:",
        ["Group.PixKey"] = "Chave PIX:",
        ["Group.PixKeyPlaceholder"] = "Digite a chave PIX",
        ["Group.BeneficiaryName"] = "Nome do beneficiário:",
        ["Group.BeneficiaryNamePlaceholder"] = "Nome completo ou razão social",
        ["Group.BeneficiaryCpf"] = "CPF do beneficiário:",
        ["Group.BankAccountNumber"] = "Número da conta bancária:",
        ["Group.BankAccountPlaceholder"] = "Número da conta (sem dígito)",
        ["Group.TestData"] = "Dados de teste",
        ["Group.PayoutSaving"] = "Salvando…",

        // Payments page
        ["Group.PaymentsTitle"] = "Pagamentos",
        ["Group.PaymentsPending"] = "Pendentes",
        ["Group.PaymentsHistory"] = "Histórico",
        ["Group.PaymentsRefresh"] = "Recarregar lista",
        ["Group.PaymentsLoading"] = "Carregando…",
        ["Group.AllParticipants"] = "Todos os participantes",
        ["Group.NoManualPayments"] = "Nenhum pagamento manual registrado ainda.",
        ["Group.NoDelinquent"] = "Nenhum jogador inadimplente. Tudo em dia!",
        ["Group.PaymentSingular"] = "pagamento",
        ["Group.PaymentPlural"] = "pagamentos",
        ["Group.GameSingular"] = "jogo",
        ["Group.GamePlural"] = "jogos",
        ["Group.Notified"] = "Notificado",
        ["Group.NotifyEmail"] = "Enviar e-mail de cobrança",
        ["Group.NotifyWhatsApp"] = "Cobrar via WhatsApp",
        ["Group.PendingMatches"] = "Partidas pendentes",
        ["Group.ViewProof"] = "Ver comprovante",
        ["Group.ViewMatch"] = "Ver partida",
        ["Group.ProofAlt"] = "Comprovante Pix",

        // Ranking page
        ["Group.RankingTitle"] = "Ranking",
        ["Group.RankingFootnote"] = "Contabiliza apenas partidas com data já encerrada.",

        // Partidas page
        ["Group.RecurringNotice"] = "Partidas geradas automaticamente (semanal)",
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
