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
