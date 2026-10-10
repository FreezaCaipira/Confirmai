using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    /// <summary>
    /// C39-B: mesa/faixa de preco de um cash game de poker. O jogador escolhe
    /// a mesa na inscricao e a confirmacao carimba o preco e a taxa dela.
    /// Criada na migration do C39-A para nao gerar duas migrations.
    /// </summary>
    public class EventPriceOption
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        /// <summary>Rotulo da mesa/faixa (ex.: "Mesa 1/2", "Buy-in leve").</summary>
        [Required]
        [StringLength(60)]
        public string Label { get; set; } = string.Empty;

        /// <summary>Preco da entrada nesta mesa (R$).</summary>
        public decimal Price { get; set; }

        /// <summary>Taxa da plataforma em % desta faixa (0-100, 2 casas).</summary>
        public decimal PlatformFeePercent { get; set; }

        /// <summary>Ordem de exibicao na inscricao.</summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Mesas desativadas nao aparecem para novas inscricoes, mas
        /// confirmacoes ja carimbadas continuam validas.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
