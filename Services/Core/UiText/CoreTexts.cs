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
        ["App.NotAuthorized"] = "You are not authorized to access this page.",

        ["Nav.Dashboard"] = "Dashboard",
        ["Nav.Marketplace"] = "Marketplace",
        ["Layout.Hello"] = "Hello",
        ["Common.Loading"] = "Loading...",

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
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["App.NotAuthorized"] = "No tienes permiso para acceder a esta pagina.",

        ["Nav.Dashboard"] = "Panel",
        ["Common.Loading"] = "Cargando...",
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
