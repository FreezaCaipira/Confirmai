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
        // Servers main page
        ["Servers.Title"] = "Game Servers",
        ["Servers.Subtitle"] = "Explore game servers and their item offers",
        ["Servers.Empty"] = "No servers found",
        ["Servers.Filter.Search"] = "Search server...",
        ["Servers.Filter.Status"] = "Status",
        ["Servers.All"] = "All",
        ["Servers.Online"] = "Online",
        ["Servers.Offline"] = "Offline",

        // ServerDetails
        ["ServerDetails.Title"] = "Server Details",
        ["ServerDetails.Players"] = "Online players",
        ["ServerDetails.Location"] = "Location",
        ["ServerDetails.Experience"] = "Experience",
        ["ServerDetails.LootRate"] = "Loot rate",
        ["ServerDetails.SpawnRate"] = "Spawn rate",
        ["ServerDetails.PvpType"] = "PvP type",
        ["ServerDetails.Offering"] = "Item offers",
        ["ServerDetails.NoOffers"] = "No active offers",
        ["ServerDetails.Admin"] = "Administrator",
        ["ServerDetails.CreatedDate"] = "Creation date",
        ["ServerDetails.Status"] = "Status",
        ["ServerDetails.Online"] = "Online",
        ["ServerDetails.Offline"] = "Offline",

        // ServerItemOffers
        ["ServerItemOffers.Title"] = "Server Offers",
        ["ServerItemOffers.Item"] = "Item",
        ["ServerItemOffers.Price"] = "Price",
        ["ServerItemOffers.Amount"] = "Quantity",
        ["ServerItemOffers.Seller"] = "Seller",
        ["ServerItemOffers.Filter.Item"] = "Item...",
        ["ServerItemOffers.Filter.MinPrice"] = "Minimum price",
        ["ServerItemOffers.Filter.MaxPrice"] = "Maximum price",
        ["ServerItemOffers.Filter.Seller"] = "Seller...",
        ["ServerItemOffers.Empty"] = "No offers found",
        ["ServerItemOffers.Purchase"] = "Purchase",
        ["ServerItemOffers.Buy"] = "Buy item",

        // ServerManage & ServerForm
        ["ServerManage.Title"] = "Manage Server",
        ["ServerManage.Edit"] = "Edit information",
        ["ServerManage.Players"] = "Manage players",
        ["ServerManage.Offers"] = "Manage offers",
        ["ServerManage.Requests"] = "Access requests",

        ["ServerForm.Title"] = "Create new server",
        ["ServerForm.EditTitle"] = "Edit server",
        ["ServerForm.Name"] = "Server name",
        ["ServerForm.Address"] = "IP address",
        ["ServerForm.Port"] = "Port",
        ["ServerForm.Location"] = "Location",
        ["ServerForm.ExperienceRate"] = "Experience rate",
        ["ServerForm.LootRate"] = "Loot rate",
        ["ServerForm.SpawnRate"] = "Spawn rate",
        ["ServerForm.PvpType"] = "PvP type",
        ["ServerForm.Description"] = "Description",
        ["ServerForm.CreateButton"] = "Create server",
        ["ServerForm.SaveButton"] = "Save changes",
        ["ServerForm.Error.NameRequired"] = "Server name required",
        ["ServerForm.Error.AddressRequired"] = "IP address required",
        ["ServerForm.Error.PortRequired"] = "Port required",
        ["ServerForm.Success"] = "Server created successfully",
        ["ServerForm.UpdateSuccess"] = "Server updated successfully",

        // ServerOfferForm
        ["ServerOfferForm.Title"] = "Create new offer",
        ["ServerOfferForm.EditTitle"] = "Edit offer",
        ["ServerOfferForm.Item"] = "Item",
        ["ServerOfferForm.Price"] = "Price (BTC)",
        ["ServerOfferForm.Amount"] = "Quantity",
        ["ServerOfferForm.CreateButton"] = "Create offer",
        ["ServerOfferForm.SaveButton"] = "Save offer",
        ["ServerOfferForm.Error.ItemRequired"] = "Select an item",
        ["ServerOfferForm.Error.PriceRequired"] = "Price required",
        ["ServerOfferForm.Error.AmountRequired"] = "Quantity required",
        ["ServerOfferForm.Success"] = "Offer created successfully",
        ["ServerOfferForm.UpdateSuccess"] = "Offer updated successfully",

        // ServerAdminRequest & ServerPlayers
        ["ServerAdminRequest.Title"] = "Access requests",
        ["ServerAdminRequest.Pending"] = "Pending",
        ["ServerAdminRequest.Approved"] = "Approved",
        ["ServerAdminRequest.Rejected"] = "Rejected",
        ["ServerAdminRequest.User"] = "User",
        ["ServerAdminRequest.Status"] = "Status",
        ["ServerAdminRequest.Approve"] = "Approve",
        ["ServerAdminRequest.Reject"] = "Reject",
        ["ServerAdminRequest.Empty"] = "No pending requests",

        ["ServerPlayers.Title"] = "Players",
        ["ServerPlayers.Whitelist"] = "Whitelist",
        ["ServerPlayers.Blacklist"] = "Blacklist",
        ["ServerPlayers.Add"] = "Add player",
        ["ServerPlayers.Remove"] = "Remove player",
        ["ServerPlayers.Confirm"] = "Are you sure?",
        ["ServerPlayers.Empty"] = "No players registered",

        // AdminServerRequests
        ["AdminServerRequests.Title"] = "Server requests",
        ["AdminServerRequests.Kicker"] = "Administrative panel",
        ["AdminServerRequests.User"] = "User",
        ["AdminServerRequests.Server"] = "Server",
        ["AdminServerRequests.Status"] = "Status",
        ["AdminServerRequests.Date"] = "Date",
        ["AdminServerRequests.Approve"] = "Approve",
        ["AdminServerRequests.Reject"] = "Reject",
        ["AdminServerRequests.Empty"] = "No pending requests",
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Servers main page
        ["Servers.Title"] = "Servidores de Juego",
        ["Servers.Subtitle"] = "Explora servidores de juego y sus ofertas de items",
        ["Servers.Empty"] = "No se encontraron servidores",
        ["Servers.Filter.Search"] = "Buscar servidor...",
        ["Servers.Filter.Status"] = "Estado",
        ["Servers.All"] = "Todos",
        ["Servers.Online"] = "En linea",
        ["Servers.Offline"] = "Desconectado",

        // ServerDetails
        ["ServerDetails.Title"] = "Detalles del Servidor",
        ["ServerDetails.Players"] = "Jugadores en linea",
        ["ServerDetails.Location"] = "Ubicacion",
        ["ServerDetails.Experience"] = "Experiencia",
        ["ServerDetails.LootRate"] = "Tasa de loot",
        ["ServerDetails.SpawnRate"] = "Tasa de spawn",
        ["ServerDetails.PvpType"] = "Tipo de PvP",
        ["ServerDetails.Offering"] = "Ofertas de items",
        ["ServerDetails.NoOffers"] = "Sin ofertas activas",
        ["ServerDetails.Admin"] = "Administrador",
        ["ServerDetails.CreatedDate"] = "Fecha de creacion",
        ["ServerDetails.Status"] = "Estado",
        ["ServerDetails.Online"] = "En linea",
        ["ServerDetails.Offline"] = "Desconectado",

        // ServerItemOffers
        ["ServerItemOffers.Title"] = "Ofertas del Servidor",
        ["ServerItemOffers.Item"] = "Item",
        ["ServerItemOffers.Price"] = "Precio",
        ["ServerItemOffers.Amount"] = "Cantidad",
        ["ServerItemOffers.Seller"] = "Vendedor",
        ["ServerItemOffers.Filter.Item"] = "Item...",
        ["ServerItemOffers.Filter.MinPrice"] = "Precio minimo",
        ["ServerItemOffers.Filter.MaxPrice"] = "Precio maximo",
        ["ServerItemOffers.Filter.Seller"] = "Vendedor...",
        ["ServerItemOffers.Empty"] = "No se encontraron ofertas",
        ["ServerItemOffers.Purchase"] = "Comprar",
        ["ServerItemOffers.Buy"] = "Comprar item",

        // ServerManage & ServerForm
        ["ServerManage.Title"] = "Gestionar Servidor",
        ["ServerManage.Edit"] = "Editar informacion",
        ["ServerManage.Players"] = "Gestionar jugadores",
        ["ServerManage.Offers"] = "Gestionar ofertas",
        ["ServerManage.Requests"] = "Solicitudes de acceso",

        ["ServerForm.Title"] = "Crear nuevo servidor",
        ["ServerForm.EditTitle"] = "Editar servidor",
        ["ServerForm.Name"] = "Nombre del servidor",
        ["ServerForm.Address"] = "Direccion IP",
        ["ServerForm.Port"] = "Puerto",
        ["ServerForm.Location"] = "Ubicacion",
        ["ServerForm.ExperienceRate"] = "Tasa de experiencia",
        ["ServerForm.LootRate"] = "Tasa de loot",
        ["ServerForm.SpawnRate"] = "Tasa de spawn",
        ["ServerForm.PvpType"] = "Tipo de PvP",
        ["ServerForm.Description"] = "Descripcion",
        ["ServerForm.CreateButton"] = "Crear servidor",
        ["ServerForm.SaveButton"] = "Guardar cambios",
        ["ServerForm.Error.NameRequired"] = "Nombre del servidor obligatorio",
        ["ServerForm.Error.AddressRequired"] = "Direccion IP obligatoria",
        ["ServerForm.Error.PortRequired"] = "Puerto obligatorio",
        ["ServerForm.Success"] = "Servidor creado exitosamente",
        ["ServerForm.UpdateSuccess"] = "Servidor actualizado exitosamente",

        // ServerOfferForm
        ["ServerOfferForm.Title"] = "Crear nueva oferta",
        ["ServerOfferForm.EditTitle"] = "Editar oferta",
        ["ServerOfferForm.Item"] = "Item",
        ["ServerOfferForm.Price"] = "Precio (BTC)",
        ["ServerOfferForm.Amount"] = "Cantidad",
        ["ServerOfferForm.CreateButton"] = "Crear oferta",
        ["ServerOfferForm.SaveButton"] = "Guardar oferta",
        ["ServerOfferForm.Error.ItemRequired"] = "Selecciona un item",
        ["ServerOfferForm.Error.PriceRequired"] = "Precio obligatorio",
        ["ServerOfferForm.Error.AmountRequired"] = "Cantidad obligatoria",
        ["ServerOfferForm.Success"] = "Oferta creada exitosamente",
        ["ServerOfferForm.UpdateSuccess"] = "Oferta actualizada exitosamente",

        // ServerAdminRequest & ServerPlayers
        ["ServerAdminRequest.Title"] = "Solicitudes de acceso",
        ["ServerAdminRequest.Pending"] = "Pendiente",
        ["ServerAdminRequest.Approved"] = "Aprobado",
        ["ServerAdminRequest.Rejected"] = "Rechazado",
        ["ServerAdminRequest.User"] = "Usuario",
        ["ServerAdminRequest.Status"] = "Estado",
        ["ServerAdminRequest.Approve"] = "Aprobar",
        ["ServerAdminRequest.Reject"] = "Rechazar",
        ["ServerAdminRequest.Empty"] = "Sin solicitudes pendientes",

        ["ServerPlayers.Title"] = "Jugadores",
        ["ServerPlayers.Whitelist"] = "Lista blanca",
        ["ServerPlayers.Blacklist"] = "Lista negra",
        ["ServerPlayers.Add"] = "Agregar jugador",
        ["ServerPlayers.Remove"] = "Eliminar jugador",
        ["ServerPlayers.Confirm"] = "Estas seguro?",
        ["ServerPlayers.Empty"] = "Sin jugadores registrados",

        // AdminServerRequests
        ["AdminServerRequests.Title"] = "Solicitudes de servidor",
        ["AdminServerRequests.Kicker"] = "Panel administrativo",
        ["AdminServerRequests.User"] = "Usuario",
        ["AdminServerRequests.Server"] = "Servidor",
        ["AdminServerRequests.Status"] = "Estado",
        ["AdminServerRequests.Date"] = "Fecha",
        ["AdminServerRequests.Approve"] = "Aprobar",
        ["AdminServerRequests.Reject"] = "Rechazar",
        ["AdminServerRequests.Empty"] = "Sin solicitudes pendientes",
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
