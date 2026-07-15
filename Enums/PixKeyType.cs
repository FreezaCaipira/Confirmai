namespace Confirmai.Enums
{
    /// <summary>Tipo de chave PIX para cadastro de conta de repasse.</summary>
    public enum PixKeyType
    {
        /// <summary>CPF (11 dígitos numéricos).</summary>
        Cpf = 1,
        
        /// <summary>CNPJ (14 dígitos numéricos).</summary>
        Cnpj = 2,
        
        /// <summary>E-mail.</summary>
        Email = 3,
        
        /// <summary>Telefone celular (+55 11 99999-9999).</summary>
        Phone = 4,
        
        /// <summary>Chave aleatória (EVP - gerada pelo banco).</summary>
        Random = 5
    }
}
