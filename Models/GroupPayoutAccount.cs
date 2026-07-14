using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    /// <summary>
    /// Conta de repasse do organizador para cobrança de taxa de serviço.
    /// Armazena a chave PIX do organizador para envio automático via API EfiBank.
    /// </summary>
    public class GroupPayoutAccount
    {
        public int Id { get; set; }

        /// <summary>Grupo vinculado a esta conta de repasse.</summary>
        public int GroupId { get; set; }
        public Group? Group { get; set; }

        /// <summary>Tipo da chave PIX (CPF, CNPJ, Email, Telefone, Aleatória).</summary>
        [Required]
        public PixKeyType PixKeyType { get; set; }

        /// <summary>Valor da chave PIX do organizador.</summary>
        [Required]
        [StringLength(140)]
        public string PixKeyValue { get; set; } = string.Empty;

        /// <summary>Nome do beneficiário (para identificação).</summary>
        [Required]
        [StringLength(200)]
        public string BeneficiaryName { get; set; } = string.Empty;

        /// <summary>Quando a conta foi cadastrada.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId do admin que cadastrou esta conta.</summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        /// <summary>Se a conta está ativa para repasse.</summary>
        public bool IsActive { get; set; } = true;
    }
}
