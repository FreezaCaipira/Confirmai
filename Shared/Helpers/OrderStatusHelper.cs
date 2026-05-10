using Confirmai.Enums;
using Confirmai.Services;

namespace Confirmai.Shared.Helpers
{
    public static class OrderStatusHelper
    {
        public static string GetStatusCssClass(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.AguardandoPagamento => "aguardandopagamento",
                PaymentStatus.Pendente => "aguardandopagamento",
                PaymentStatus.Pago => "pago",
                PaymentStatus.AguardandoEntrega => "aguardandoentrega",
                PaymentStatus.AguardandoEntregaInGame => "aguardandoentregaingame",
                PaymentStatus.Entregue => "entregue",
                PaymentStatus.Finalizado => "finalizado",
                PaymentStatus.Reembolsado => "finalizado",
                PaymentStatus.Cancelado => "cancelado",
                PaymentStatus.Falha => "cancelado",
                PaymentStatus.Disputa => "disputa",
                PaymentStatus.AguardandoRevisaoAdm => "aguardandorevisaoadm",
                _ => "outro"
            };
        }

        public static string GetStatusKey(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.AguardandoPagamento => "OrderStatus.AguardandoPagamento",
                PaymentStatus.Pago => "OrderStatus.Pago",
                PaymentStatus.AguardandoEntrega => "OrderStatus.AguardandoEntrega",
                PaymentStatus.AguardandoEntregaInGame => "OrderStatus.AguardandoEntregaInGame",
                PaymentStatus.Entregue => "OrderStatus.Entregue",
                PaymentStatus.Finalizado => "OrderStatus.Finalizado",
                PaymentStatus.Cancelado => "OrderStatus.Cancelado",
                PaymentStatus.Disputa => "OrderStatus.Disputa",
                PaymentStatus.AguardandoRevisaoAdm => "OrderStatus.AguardandoRevisaoAdm",
                PaymentStatus.Pendente => "OrderStatus.Pendente",
                PaymentStatus.Reembolsado => "OrderStatus.Reembolsado",
                PaymentStatus.Falha => "OrderStatus.Falha",
                _ => "OrderStatus.Outro"
            };
        }

        public static string GetStatusText(PaymentStatus status, UiTextService textService)
        {
            var key = GetStatusKey(status);
            var localized = textService[key];

            return string.Equals(localized, key, StringComparison.Ordinal)
                ? GetStatusText(status)
                : localized;
        }

        public static string GetStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.AguardandoPagamento => "Aguardando pagamento",
                PaymentStatus.Pago => "Pago",
                PaymentStatus.AguardandoEntrega => "Aguardando entrega",
                PaymentStatus.AguardandoEntregaInGame => "Aguardando entrega in-game",
                PaymentStatus.Entregue => "Entregue",
                PaymentStatus.Finalizado => "Finalizada",
                PaymentStatus.Cancelado => "Cancelado",
                PaymentStatus.Disputa => "Em disputa",
                PaymentStatus.AguardandoRevisaoAdm => "Aguardando revisao ADM",
                PaymentStatus.Pendente => "Pendente",
                PaymentStatus.Reembolsado => "Reembolsado",
                PaymentStatus.Falha => "Falha",
                _ => "Outro"
            };
        }
    }
}

