namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Server & marketplace text domain: game server listings, player management,
/// marketplace operations, form UI, and marketplace features.
/// Covers Servers, ServerDetails, ServerItemOffers, ServerManage, ServerForm, etc.
/// </summary>
internal static class ServerTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Servers main page
        ["Servers.Title"] = "Servidores",
        ["Servers.Subtitle"] = "Explore game servers e suas ofertas de itens",
        ["Servers.Empty"] = "Nenhum servidor encontrado",
        ["Servers.Filter.Search"] = "Buscar servidor...",
        ["Servers.Filter.Status"] = "Status",
        ["Servers.All"] = "Todos",
        ["Servers.Online"] = "Online",
        ["Servers.Offline"] = "Offline",

        // ServerDetails
        ["ServerDetails.Title"] = "Detalhes do Servidor",
        ["ServerDetails.Players"] = "Jogadores online",
        ["ServerDetails.Location"] = "Localizacao",
        ["ServerDetails.Experience"] = "Experiencia",
        ["ServerDetails.LootRate"] = "Taxa de loot",
        ["ServerDetails.SpawnRate"] = "Taxa de spawn",
        ["ServerDetails.PvpType"] = "Tipo de PvP",
        ["ServerDetails.Offering"] = "Ofertas de itens",
        ["ServerDetails.NoOffers"] = "Nenhuma oferta ativa",
        ["ServerDetails.Admin"] = "Administrador",
        ["ServerDetails.CreatedDate"] = "Data de criacao",
        ["ServerDetails.Status"] = "Status",
        ["ServerDetails.Online"] = "Online",
        ["ServerDetails.Offline"] = "Offline",

        // ServerItemOffers
        ["ServerItemOffers.Title"] = "Ofertas do Servidor",
        ["ServerItemOffers.Item"] = "Item",
        ["ServerItemOffers.Price"] = "Preco",
        ["ServerItemOffers.Amount"] = "Quantidade",
        ["ServerItemOffers.Seller"] = "Vendedor",
        ["ServerItemOffers.Filter.Item"] = "Item...",
        ["ServerItemOffers.Filter.MinPrice"] = "Preco minimo",
        ["ServerItemOffers.Filter.MaxPrice"] = "Preco maximo",
        ["ServerItemOffers.Filter.Seller"] = "Vendedor...",
        ["ServerItemOffers.Empty"] = "Nenhuma oferta encontrada",
        ["ServerItemOffers.Purchase"] = "Comprar",
        ["ServerItemOffers.Buy"] = "Comprar item",

        // ServerManage & ServerForm
        ["ServerManage.Title"] = "Gerenciar Servidor",
        ["ServerManage.Edit"] = "Editar informacoes",
        ["ServerManage.Players"] = "Gerenciar jogadores",
        ["ServerManage.Offers"] = "Gerenciar ofertas",
        ["ServerManage.Requests"] = "Requisicoes de acesso",

        ["ServerForm.Title"] = "Criar novo servidor",
        ["ServerForm.EditTitle"] = "Editar servidor",
        ["ServerForm.Name"] = "Nome do servidor",
        ["ServerForm.Address"] = "Endereco IP",
        ["ServerForm.Port"] = "Porta",
        ["ServerForm.Location"] = "Localizacao",
        ["ServerForm.ExperienceRate"] = "Taxa de experiencia",
        ["ServerForm.LootRate"] = "Taxa de loot",
        ["ServerForm.SpawnRate"] = "Taxa de spawn",
        ["ServerForm.PvpType"] = "Tipo de PvP",
        ["ServerForm.Description"] = "Descricao",
        ["ServerForm.CreateButton"] = "Criar servidor",
        ["ServerForm.SaveButton"] = "Salvar alteracoes",
        ["ServerForm.Error.NameRequired"] = "Nome do servidor obrigatorio",
        ["ServerForm.Error.AddressRequired"] = "Endereco IP obrigatorio",
        ["ServerForm.Error.PortRequired"] = "Porta obrigatoria",
        ["ServerForm.Success"] = "Servidor criado com sucesso",
        ["ServerForm.UpdateSuccess"] = "Servidor atualizado com sucesso",

        // ServerOfferForm
        ["ServerOfferForm.Title"] = "Criar nova oferta",
        ["ServerOfferForm.EditTitle"] = "Editar oferta",
        ["ServerOfferForm.Item"] = "Item",
        ["ServerOfferForm.Price"] = "Preco (BTC)",
        ["ServerOfferForm.Amount"] = "Quantidade",
        ["ServerOfferForm.CreateButton"] = "Criar oferta",
        ["ServerOfferForm.SaveButton"] = "Salvar oferta",
        ["ServerOfferForm.Error.ItemRequired"] = "Selecione um item",
        ["ServerOfferForm.Error.PriceRequired"] = "Preco obrigatorio",
        ["ServerOfferForm.Error.AmountRequired"] = "Quantidade obrigatoria",
        ["ServerOfferForm.Success"] = "Oferta criada com sucesso",
        ["ServerOfferForm.UpdateSuccess"] = "Oferta atualizada com sucesso",

        // ServerAdminRequest & ServerPlayers
        ["ServerAdminRequest.Title"] = "Requisicoes de acesso",
        ["ServerAdminRequest.Pending"] = "Pendente",
        ["ServerAdminRequest.Approved"] = "Aprovado",
        ["ServerAdminRequest.Rejected"] = "Rejeitado",
        ["ServerAdminRequest.User"] = "Usuario",
        ["ServerAdminRequest.Status"] = "Status",
        ["ServerAdminRequest.Approve"] = "Aprovar",
        ["ServerAdminRequest.Reject"] = "Rejeitar",
        ["ServerAdminRequest.Empty"] = "Nenhuma requisicao pendente",

        ["ServerPlayers.Title"] = "Jogadores",
        ["ServerPlayers.Whitelist"] = "Whitelist",
        ["ServerPlayers.Blacklist"] = "Blacklist",
        ["ServerPlayers.Add"] = "Adicionar jogador",
        ["ServerPlayers.Remove"] = "Remover jogador",
        ["ServerPlayers.Confirm"] = "Tem certeza?",
        ["ServerPlayers.Empty"] = "Nenhum jogador cadastrado",

        // AdminServerRequests
        ["AdminServerRequests.Title"] = "Requisicoes de servidor",
        ["AdminServerRequests.Kicker"] = "Painel administrativo",
        ["AdminServerRequests.User"] = "Usuario",
        ["AdminServerRequests.Server"] = "Servidor",
        ["AdminServerRequests.Status"] = "Status",
        ["AdminServerRequests.Date"] = "Data",
        ["AdminServerRequests.Approve"] = "Aprovar",
        ["AdminServerRequests.Reject"] = "Rejeitar",
        ["AdminServerRequests.Empty"] = "Nenhuma requisicao pendente",
    };

    /// <summary>EN-US English (United States) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Stubs - extend with English translations
        ["Servers.Title"] = "Game Servers",
        ["ServerDetails.Title"] = "Server Details",
        ["ServerForm.Title"] = "Create new server",
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Stubs - extend with Spanish translations
        ["Servers.Title"] = "Servidores de Juego",
        ["ServerDetails.Title"] = "Detalles del Servidor",
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
