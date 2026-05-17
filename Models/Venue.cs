using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class Venue
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public VenueType Type { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(2)]
        public string StateCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Usuário que criou o registro (apenas auditoria).</summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        /// <summary>Usuário responsável pela gestão desta quadra (opcional).</summary>
        [StringLength(450)]
        public string? VenueAdminUserId { get; set; }

        [ForeignKey(nameof(VenueAdminUserId))]
        public ApplicationUser? VenueAdmin { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<MatchSchedule> Schedules { get; set; } = new List<MatchSchedule>();
    }
}
