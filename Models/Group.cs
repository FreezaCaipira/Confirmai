using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public Sport Sport { get; set; }

        [StringLength(120)]
        public string City { get; set; } = string.Empty;

        [StringLength(2)]
        public string StateCode { get; set; } = string.Empty;

        [StringLength(300)]
        public string? LogoPath { get; set; }

        [StringLength(200)]
        public string? WebsiteUrl { get; set; }

        /// <summary>Código de convite único para novos membros entrarem no grupo via link.</summary>
        [Required]
        [StringLength(12)]
        public string InviteCode { get; set; } = string.Empty;

        /// <summary>Quando verdadeiro, somente membros do grupo podem confirmar presença nos eventos.</summary>
        public bool IsPrivate { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // ── Feature flags ────────────────────────────────────────────────────

        /// <summary>Habilita o ranking pós-partida (mensal/anual) para o grupo.</summary>
        public bool EnablePostMatchRanking { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        /// <summary>
        /// UserId do admin cujo PixKey será exibido como destino de pagamento dos eventos deste grupo.
        /// Se nulo, usa o primeiro admin encontrado que tenha PixKey configurado.
        /// </summary>
        [StringLength(450)]
        public string? PixReceiverUserId { get; set; }
        public ApplicationUser? PixReceiverUser { get; set; }

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
