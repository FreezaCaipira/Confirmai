namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Core UI text domain: Navigation, Layout, Common, Error, Index, Breadcrumb
/// Covers fundamental app text strings used across all pages.
/// </summary>
internal static class CoreTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation
        ["Nav.Dashboard"] = "Dashboard",
        ["Nav.Marketplace"] = "Marketplace",
        ["Nav.MyProducts"] = "Meus Produtos",
        ["Nav.InvoiceHistory"] = "Historico de faturas",
        ["Nav.Login"] = "Entrar",
        ["Nav.Register"] = "Registrar",
        ["Nav.Admin"] = "Admin",
        ["Nav.AdminLanguages"] = "Idiomas",
        ["Nav.About"] = "Sobre",
        ["Nav.Contact"] = "Contato",
        ["Nav.Payments"] = "Pagamentos",
        ["Nav.Integration"] = "Integracao",
        ["Nav.Mailbox"] = "Caixa de Entrada",
        ["Nav.Profile"] = "Perfil",

        // Layout
        ["Layout.HeaderQuoteTitle"] = "Moeda ativa para exibicao de cotacao",
        ["Layout.HeaderQuote"] = "Cotacao",
        ["Layout.Language"] = "Idioma",
        ["Layout.LanguagePtBr"] = "Portugues (Brasil)",
        ["Layout.LanguageEnUs"] = "Ingles (Estados Unidos)",
        ["Layout.LanguageEsEs"] = "Espanhol",
        ["Layout.Hello"] = "Ola",
        ["Layout.UserFallback"] = "Usuario",
        ["Layout.Logout"] = "Sair",
        ["Layout.WelcomeVisitor"] = "Bem-vindo, visitante!",
        ["Layout.NewsTicker1"] = "Novidades do Confirmai",
        ["Layout.NewsTicker2"] = "Confirme sua presença nos jogos",
        ["Layout.NewsTicker3"] = "Novos recursos disponiveis",
        ["Layout.NewsTicker4"] = "Participe da comunidade",

        // Index
        ["Index.TabExplore"] = "Explorar",
        ["Index.TabMyGames"] = "Meus Jogos",
        ["Index.CityLabel"] = "Cidade",
        ["Index.OtherCity"] = "Outra cidade...",
        ["Index.FutsalCardDesc"] = "Peladas, campeonatos e escolinhas na quadra",
        ["Index.PokerCardDesc"] = "Torneios, cash games e ligas semanais",
        ["Index.ViewMatches"] = "Ver partidas →",
        ["Index.ViewTournaments"] = "Ver torneios →",
        ["Index.LoadingConfirmations"] = "Carregando suas confirmações…",
        ["Index.NoConfirmations"] = "Você ainda não confirmou presença em nenhum evento.",
        ["Index.ViewFutsal"] = "Ver Futsal",
        ["Index.ViewPoker"] = "Ver Poker",
        ["Index.Upcoming"] = "Próximos",
        ["Index.Confirmed"] = "Confirmado",
        ["Index.ConfirmedCount"] = "confirmados",
        ["Index.Previous"] = "Anteriores",
        ["Index.NoPrevious"] = "Nenhuma partida anterior.",
        ["Index.Cancelled"] = "Cancelado",
        ["Index.NoCancelled"] = "Nenhuma partida cancelada.",
        ["Index.LoginRequired"] = "Faça login",
        ["Index.LoginRequiredSuffix"] = "para ver seus jogos.",
        ["Index.Goalkeeper"] = "Goleiro",
        ["Index.Line"] = "Linha",
        ["Index.Enter"] = "Entrar",

        // Common
        ["Common.Loading"] = "Carregando...",
        ["Common.Error"] = "Erro",
        ["Common.Success"] = "Sucesso",
        ["Common.Confirm"] = "Confirmar",
        ["Common.Cancel"] = "Cancelar",
        ["Common.Delete"] = "Deletar",
        ["Common.Edit"] = "Editar",
        ["Common.Save"] = "Salvar",
        ["Common.Close"] = "Fechar",
        ["Common.Back"] = "Voltar",
        ["Common.Next"] = "Proximo",
        ["Common.Previous"] = "Anterior",
        ["Common.Search"] = "Buscar",
        ["Common.Filter"] = "Filtro",
        ["Common.Clear"] = "Limpar",
        ["Common.Export"] = "Exportar",
        ["Common.Import"] = "Importar",
        ["Common.NotFound"] = "Nao encontrado",
        ["Common.NoData"] = "Sem dados",
        ["Common.Optional"] = "Opcional",
        ["Common.Required"] = "Obrigatorio",

        // App
        ["App.NotAuthorized"] = "Voce nao tem permissao para acessar esta pagina.",

        // Breadcrumb
        ["Breadcrumb.Home"] = "Inicio",
        ["Breadcrumb.Admin"] = "Admin",
        ["Breadcrumb.Users"] = "Usuarios",
        ["Breadcrumb.Payments"] = "Pagamentos",
        ["Breadcrumb.Logs"] = "Logs",
        ["Breadcrumb.Edit"] = "Editar",
        ["Breadcrumb.View"] = "Visualizar",
        ["Breadcrumb.Buy"] = "Comprar",
        ["Breadcrumb.Items"] = "Itens",
        ["Breadcrumb.Products"] = "Produtos",
        ["Breadcrumb.About"] = "Sobre",
        ["Breadcrumb.Contact"] = "Contato",
        ["Breadcrumb.Dashboard"] = "Dashboard",
        ["Breadcrumb.Marketplace"] = "Marketplace",
        ["Breadcrumb.Languages"] = "Idiomas",

        // Error
        ["Error.Title"] = "Erro",
        ["Error.Message"] = "Algo deu errado",
        ["Error.NotFound"] = "Pagina nao encontrada",
        ["Error.AccessDenied"] = "Acesso negado",
        ["Error.InternalServer"] = "Erro interno do servidor",
    };

    /// <summary>EN-US English (United States) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation
        ["Nav.Dashboard"] = "Dashboard",
        ["Nav.Marketplace"] = "Marketplace",
        ["Nav.MyProducts"] = "My Products",
        ["Nav.InvoiceHistory"] = "Invoice History",
        ["Nav.Login"] = "Login",
        ["Nav.Register"] = "Register",
        ["Nav.Admin"] = "Admin",
        ["Nav.AdminLanguages"] = "Languages",
        ["Nav.About"] = "About",
        ["Nav.Contact"] = "Contact",
        ["Nav.Payments"] = "Payments",

        // Layout
        ["Layout.HeaderQuoteTitle"] = "Active currency for quote display",
        ["Layout.HeaderQuote"] = "Quote",
        ["Layout.Language"] = "Language",
        ["Layout.LanguagePtBr"] = "Portuguese (Brazil)",
        ["Layout.LanguageEnUs"] = "English (United States)",
        ["Layout.LanguageEsEs"] = "Spanish",
        ["Layout.Hello"] = "Hello",
        ["Layout.UserFallback"] = "User",
        ["Layout.Logout"] = "Logout",
        ["Layout.WelcomeVisitor"] = "Welcome, visitor!",
        ["Layout.NewsTicker1"] = "Confirmai News",
        ["Layout.NewsTicker2"] = "Confirm your presence in games",
        ["Layout.NewsTicker3"] = "New features available",
        ["Layout.NewsTicker4"] = "Join the community",

        // Index
        ["Index.TabExplore"] = "Explore",
        ["Index.TabMyGames"] = "My Games",
        ["Index.CityLabel"] = "City",
        ["Index.OtherCity"] = "Other city...",
        ["Index.FutsalCardDesc"] = "Pickup games, tournaments and training sessions",
        ["Index.PokerCardDesc"] = "Tournaments, cash games and weekly leagues",
        ["Index.ViewMatches"] = "View matches →",
        ["Index.ViewTournaments"] = "View tournaments →",
        ["Index.LoadingConfirmations"] = "Loading your confirmations…",
        ["Index.NoConfirmations"] = "You haven't confirmed presence in any event yet.",
        ["Index.ViewFutsal"] = "View Futsal",
        ["Index.ViewPoker"] = "View Poker",
        ["Index.Upcoming"] = "Upcoming",
        ["Index.Confirmed"] = "Confirmed",
        ["Index.ConfirmedCount"] = "confirmed",
        ["Index.Previous"] = "Previous",
        ["Index.NoPrevious"] = "No previous matches.",
        ["Index.Cancelled"] = "Cancelled",
        ["Index.NoCancelled"] = "No cancelled matches.",
        ["Index.LoginRequired"] = "Log in",
        ["Index.LoginRequiredSuffix"] = "to view your games.",
        ["Index.Goalkeeper"] = "Goalkeeper",
        ["Index.Line"] = "Line",
        ["Index.Enter"] = "Enter",

        // Common
        ["Common.Loading"] = "Loading...",
        ["Common.Error"] = "Error",
        ["Common.Success"] = "Success",
        ["Common.Confirm"] = "Confirm",
        ["Common.Cancel"] = "Cancel",
        ["Common.Delete"] = "Delete",
        ["Common.Edit"] = "Edit",
        ["Common.Save"] = "Save",
        ["Common.Close"] = "Close",
        ["Common.Back"] = "Back",
        ["Common.Next"] = "Next",
        ["Common.Previous"] = "Previous",
        ["Common.Search"] = "Search",
        ["Common.Filter"] = "Filter",
        ["Common.Clear"] = "Clear",
        ["Common.Export"] = "Export",
        ["Common.Import"] = "Import",
        ["Common.NotFound"] = "Not found",
        ["Common.NoData"] = "No data",
        ["Common.Optional"] = "Optional",
        ["Common.Required"] = "Required",

        // App
        ["App.NotAuthorized"] = "You are not authorized to access this page.",

        // Breadcrumb
        ["Breadcrumb.Home"] = "Home",
        ["Breadcrumb.Admin"] = "Admin",
        ["Breadcrumb.Users"] = "Users",
        ["Breadcrumb.Payments"] = "Payments",
        ["Breadcrumb.Logs"] = "Logs",
        ["Breadcrumb.Edit"] = "Edit",
        ["Breadcrumb.View"] = "View",
        ["Breadcrumb.Buy"] = "Buy",
        ["Breadcrumb.Items"] = "Items",
        ["Breadcrumb.Products"] = "Products",
        ["Breadcrumb.About"] = "About",
        ["Breadcrumb.Contact"] = "Contact",
        ["Breadcrumb.Dashboard"] = "Dashboard",
        ["Breadcrumb.Marketplace"] = "Marketplace",
        ["Breadcrumb.Languages"] = "Languages",

        // Error
        ["Error.Title"] = "Error",
        ["Error.Message"] = "Something went wrong",
        ["Error.NotFound"] = "Page not found",
        ["Error.AccessDenied"] = "Access denied",
        ["Error.InternalServer"] = "Internal server error",
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation
        ["Nav.Dashboard"] = "Panel",
        ["Nav.Marketplace"] = "Mercado",
        ["Nav.MyProducts"] = "Mis Productos",
        ["Nav.InvoiceHistory"] = "Historial de facturas",
        ["Nav.Login"] = "Iniciar sesion",
        ["Nav.Register"] = "Registrarse",
        ["Nav.Admin"] = "Admin",
        ["Nav.AdminLanguages"] = "Idiomas",
        ["Nav.About"] = "Acerca de",
        ["Nav.Contact"] = "Contacto",
        ["Nav.Payments"] = "Pagos",
        ["Nav.Integration"] = "Integracion",
        ["Nav.Mailbox"] = "Bandeja de entrada",
        ["Nav.Profile"] = "Perfil",

        // Layout
        ["Layout.HeaderQuoteTitle"] = "Moneda activa para mostrar cotizacion",
        ["Layout.HeaderQuote"] = "Cotizacion",
        ["Layout.Language"] = "Idioma",
        ["Layout.LanguagePtBr"] = "Portugues (Brasil)",
        ["Layout.LanguageEnUs"] = "Ingles (Estados Unidos)",
        ["Layout.LanguageEsEs"] = "Espanol",
        ["Layout.Hello"] = "Hola",
        ["Layout.UserFallback"] = "Usuario",
        ["Layout.Logout"] = "Cerrar sesion",
        ["Layout.WelcomeVisitor"] = "Bienvenido, visitante!",
        ["Layout.NewsTicker1"] = "Noticias de Confirmai",
        ["Layout.NewsTicker2"] = "Confirma tu presencia en los juegos",
        ["Layout.NewsTicker3"] = "Nuevas caracteristicas disponibles",
        ["Layout.NewsTicker4"] = "Unete a la comunidad",

        // Index
        ["Index.TabExplore"] = "Explorar",
        ["Index.TabMyGames"] = "Mis Juegos",
        ["Index.CityLabel"] = "Ciudad",
        ["Index.OtherCity"] = "Otra ciudad...",
        ["Index.FutsalCardDesc"] = "Partidos, torneos y escuelas en la cancha",
        ["Index.PokerCardDesc"] = "Torneos, cash games y ligas semanales",
        ["Index.ViewMatches"] = "Ver partidos →",
        ["Index.ViewTournaments"] = "Ver torneos →",
        ["Index.LoadingConfirmations"] = "Cargando tus confirmaciones…",
        ["Index.NoConfirmations"] = "Aun no has confirmado presencia en ningun evento.",
        ["Index.ViewFutsal"] = "Ver Futsal",
        ["Index.ViewPoker"] = "Ver Poker",
        ["Index.Upcoming"] = "Proximos",
        ["Index.Confirmed"] = "Confirmado",
        ["Index.ConfirmedCount"] = "confirmados",
        ["Index.Previous"] = "Anteriores",
        ["Index.NoPrevious"] = "Ningun partido anterior.",
        ["Index.Cancelled"] = "Cancelado",
        ["Index.NoCancelled"] = "Ningun partido cancelado.",
        ["Index.LoginRequired"] = "Inicia sesion",
        ["Index.LoginRequiredSuffix"] = "para ver tus juegos.",
        ["Index.Goalkeeper"] = "Portero",
        ["Index.Line"] = "Linea",
        ["Index.Enter"] = "Entrar",

        // Common
        ["Common.Loading"] = "Cargando...",
        ["Common.Error"] = "Error",
        ["Common.Success"] = "Exito",
        ["Common.Confirm"] = "Confirmar",
        ["Common.Cancel"] = "Cancelar",
        ["Common.Delete"] = "Eliminar",
        ["Common.Edit"] = "Editar",
        ["Common.Save"] = "Guardar",
        ["Common.Close"] = "Cerrar",
        ["Common.Back"] = "Atras",
        ["Common.Next"] = "Siguiente",
        ["Common.Previous"] = "Anterior",
        ["Common.Search"] = "Buscar",
        ["Common.Filter"] = "Filtro",
        ["Common.Clear"] = "Limpiar",
        ["Common.Export"] = "Exportar",
        ["Common.Import"] = "Importar",
        ["Common.NotFound"] = "No encontrado",
        ["Common.NoData"] = "Sin datos",
        ["Common.Optional"] = "Opcional",
        ["Common.Required"] = "Obligatorio",

        // App
        ["App.NotAuthorized"] = "No tienes permiso para acceder a esta pagina.",

        // Breadcrumb
        ["Breadcrumb.Home"] = "Inicio",
        ["Breadcrumb.Admin"] = "Admin",
        ["Breadcrumb.Users"] = "Usuarios",
        ["Breadcrumb.Payments"] = "Pagos",
        ["Breadcrumb.Logs"] = "Registros",
        ["Breadcrumb.Edit"] = "Editar",
        ["Breadcrumb.View"] = "Ver",
        ["Breadcrumb.Buy"] = "Comprar",
        ["Breadcrumb.Items"] = "Articulos",
        ["Breadcrumb.Products"] = "Productos",
        ["Breadcrumb.About"] = "Acerca de",
        ["Breadcrumb.Contact"] = "Contacto",
        ["Breadcrumb.Dashboard"] = "Panel",
        ["Breadcrumb.Marketplace"] = "Mercado",
        ["Breadcrumb.Languages"] = "Idiomas",

        // Error
        ["Error.Title"] = "Error",
        ["Error.Message"] = "Algo salio mal",
        ["Error.NotFound"] = "Pagina no encontrada",
        ["Error.AccessDenied"] = "Acceso denegado",
        ["Error.InternalServer"] = "Error interno del servidor",
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
