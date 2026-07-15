namespace Confirmai.Enums
{
    /// <summary>Estado do repasse de pagamento ao organizador.</summary>
    public enum PayoutStatus
    {
        /// <summary>Repasse pendente (aguardando processamento).</summary>
        Pending = 0,
        
        /// <summary>Repasse enviado com sucesso (aguardando confirmação do banco).</summary>
        Sent = 1,
        
        /// <summary>Repasse confirmado pelo banco (dinheiro entregue ao organizador).</summary>
        Confirmed = 2,
        
        /// <summary>Repasse falhou (chave inválida, saldo insuficiente, etc.).</summary>
        Failed = 3,
        
        /// <summary>Repasse em retry (tentativa automática após falha).</summary>
        Retrying = 4
    }
}
