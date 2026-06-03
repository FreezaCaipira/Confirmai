namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Payment text domain: checkout flow, order history, payment status,
/// product details, and payment-related messaging.
/// Covers PaymentBuy, PaymentHistory, PaymentView, PaymentDetails, OrderStatus, etc.
/// </summary>
internal static class PaymentTexts
{
    /// <summary>PT-BR Portuguese (Brazil) strings</summary>
    public static IReadOnlyDictionary<string, string> PtBr => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // PaymentBuy
        ["PaymentBuy.Title"] = "Finalizar compra",
        ["PaymentBuy.Product"] = "Produto",
        ["PaymentBuy.Price"] = "Preco",
        ["PaymentBuy.Description"] = "Descricao",
        ["PaymentBuy.Seller"] = "Vendedor",
        ["PaymentBuy.Quantity"] = "Quantidade",
        ["PaymentBuy.Total"] = "Total",
        ["PaymentBuy.Method"] = "Metodo de pagamento",
        ["PaymentBuy.SelectMethod"] = "Selecione um metodo",
        ["PaymentBuy.Confirm"] = "Confirmar pagamento",
        ["PaymentBuy.Cancel"] = "Cancelar",
        ["PaymentBuy.Success"] = "Pagamento realizado com sucesso",
        ["PaymentBuy.Error"] = "Erro ao processar pagamento",
        ["PaymentBuy.InvalidProduct"] = "Produto invalido",
        ["PaymentBuy.InvalidQuantity"] = "Quantidade invalida",
        ["PaymentBuy.InsufficientFunds"] = "Saldo insuficiente",
        ["PaymentBuy.Processing"] = "Processando pagamento...",

        // PaymentHistory
        ["PaymentHistory.Title"] = "Historico de faturas",
        ["PaymentHistory.Subtitle"] = "Acompanhe suas transacoes e pagamentos",
        ["PaymentHistory.Empty"] = "Nenhuma transacao encontrada",
        ["PaymentHistory.Date"] = "Data",
        ["PaymentHistory.Product"] = "Produto",
        ["PaymentHistory.Amount"] = "Valor",
        ["PaymentHistory.Status"] = "Status",
        ["PaymentHistory.Action"] = "Acao",
        ["PaymentHistory.View"] = "Visualizar",
        ["PaymentHistory.Filter.Status"] = "Status",
        ["PaymentHistory.Filter.Product"] = "Produto...",
        ["PaymentHistory.Filter.MinAmount"] = "Valor minimo",
        ["PaymentHistory.Filter.MaxAmount"] = "Valor maximo",
        ["PaymentHistory.AllStatuses"] = "Todos os status",

        // PaymentView
        ["PaymentView.Title"] = "Detalhes da transacao",
        ["PaymentView.Kicker"] = "Historico de faturas",
        ["PaymentView.TransactionId"] = "ID da transacao",
        ["PaymentView.Date"] = "Data",
        ["PaymentView.Product"] = "Produto",
        ["PaymentView.Seller"] = "Vendedor",
        ["PaymentView.Buyer"] = "Comprador",
        ["PaymentView.Amount"] = "Valor",
        ["PaymentView.Method"] = "Metodo de pagamento",
        ["PaymentView.Status"] = "Status",
        ["PaymentView.Back"] = "Voltar ao historico",
        ["PaymentView.Download"] = "Baixar recibo",
        ["PaymentView.NotFound"] = "Transacao nao encontrada",

        // PaymentDetails
        ["PaymentDetails.Title"] = "Detalhes do pagamento",
        ["PaymentDetails.Summary"] = "Resumo",
        ["PaymentDetails.Subtotal"] = "Subtotal",
        ["PaymentDetails.Tax"] = "Imposto",
        ["PaymentDetails.Fee"] = "Taxa de processamento",
        ["PaymentDetails.Discount"] = "Desconto",
        ["PaymentDetails.Total"] = "Total",
        ["PaymentDetails.PaymentMethod"] = "Metodo de pagamento",
        ["PaymentDetails.TransactionId"] = "ID da transacao",
        ["PaymentDetails.Date"] = "Data",
        ["PaymentDetails.TimeToRefund"] = "Tempo para reembolso",
        ["PaymentDetails.RefundPolicy"] = "Politica de reembolso",
        ["PaymentDetails.Confirm"] = "Confirmar e enviar",
        ["PaymentDetails.Cancel"] = "Cancelar",

        // OrderStatus
        ["OrderStatus.Pending"] = "Pendente",
        ["OrderStatus.Confirmed"] = "Confirmado",
        ["OrderStatus.Processing"] = "Processando",
        ["OrderStatus.Shipped"] = "Enviado",
        ["OrderStatus.Delivered"] = "Entregue",
        ["OrderStatus.Cancelled"] = "Cancelado",
        ["OrderStatus.Refunded"] = "Reembolsado",
        ["OrderStatus.Failed"] = "Falha no pagamento",
        ["OrderStatus.Disputed"] = "Disputado",
        ["OrderStatus.OnHold"] = "Em espera",
        ["OrderStatus.PartiallyShipped"] = "Parcialmente enviado",
        ["OrderStatus.WaitingForPayment"] = "Aguardando pagamento",
        ["OrderStatus.ReviewRequired"] = "Revisao necessaria",

        // Products (marketplace product strings)
        ["Products.Title"] = "Produtos",
        ["Products.Search"] = "Buscar produtos...",
        ["Products.Filter.Category"] = "Categoria",
        ["Products.Filter.Price"] = "Preco",
        ["Products.Filter.Seller"] = "Vendedor",
        ["Products.Filter.Delivery"] = "Tipo de entrega",
        ["Products.AllCategories"] = "Todas as categorias",
        ["Products.Physical"] = "Entrega fisica",
        ["Products.Digital"] = "Entrega digital",
        ["Products.AllDeliveryTypes"] = "Todos os tipos",
        ["Products.Empty"] = "Nenhum produto encontrado",
        ["Products.SortBy"] = "Ordenar por",
        ["Products.SortNewest"] = "Mais recentes",
        ["Products.SortPrice"] = "Preco",
        ["Products.SortPopular"] = "Mais populares",
        ["Products.Add"] = "Anunciar produto",

        // ProductForm
        ["ProductForm.Title"] = "Anunciar novo produto",
        ["ProductForm.EditTitle"] = "Editar produto",
        ["ProductForm.Name"] = "Nome do produto",
        ["ProductForm.Category"] = "Categoria",
        ["ProductForm.Description"] = "Descricao",
        ["ProductForm.Price"] = "Preco (BTC)",
        ["ProductForm.Delivery"] = "Tipo de entrega",
        ["ProductForm.DeliveryPhysical"] = "Entrega fisica",
        ["ProductForm.DeliveryDigital"] = "Entrega digital",
        ["ProductForm.DeliveryAgent"] = "Entregador",
        ["ProductForm.Images"] = "Imagens",
        ["ProductForm.Create"] = "Anunciar",
        ["ProductForm.Save"] = "Salvar",
        ["ProductForm.Cancel"] = "Cancelar",
        ["ProductForm.Error.NameRequired"] = "Nome obrigatorio",
        ["ProductForm.Error.PriceRequired"] = "Preco obrigatorio",
        ["ProductForm.Error.CategoryRequired"] = "Categoria obrigatoria",
        ["ProductForm.Error.PriceInvalid"] = "Preco invalido",
        ["ProductForm.Success"] = "Produto anunciado com sucesso",
        ["ProductForm.UpdateSuccess"] = "Produto atualizado com sucesso",

        // Marketplace
        ["Marketplace.Title"] = "Marketplace",
        ["Marketplace.Subtitle"] = "Compre e venda com seguranca",
        ["Marketplace.Featured"] = "Destaque",
        ["Marketplace.Recent"] = "Recentes",
        ["Marketplace.MyListings"] = "Meus anuncios",
        ["Marketplace.MyPurchases"] = "Minhas compras",
        ["Marketplace.MySales"] = "Minhas vendas",
    };

    /// <summary>EN-US English (United States) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EnUs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Stubs - extend with English translations
        ["PaymentBuy.Title"] = "Checkout",
        ["PaymentHistory.Title"] = "Invoice History",
        ["OrderStatus.Pending"] = "Pending",
        ["OrderStatus.Confirmed"] = "Confirmed",
        ["Products.Title"] = "Products",
        ["Marketplace.Title"] = "Marketplace",
    };

    /// <summary>ES-ES Spanish (Spain) strings - stub for extension</summary>
    public static IReadOnlyDictionary<string, string> EsEs => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Stubs - extend with Spanish translations
        ["PaymentBuy.Title"] = "Finalizar compra",
        ["PaymentHistory.Title"] = "Historial de facturas",
        ["Marketplace.Title"] = "Mercado",
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
