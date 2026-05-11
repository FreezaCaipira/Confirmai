using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class Event
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public Sport Sport { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public DateTime StartsAt { get; set; }

        public int MaxPlayers { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        // Campos específicos: Futsal
        public int? MaxGoalkeepers { get; set; }

        // Campos específicos: Poker
        [StringLength(50)]
        public string? BuyIn { get; set; }

        [StringLength(50)]
        public string? Rebuy { get; set; }

        [StringLength(50)]
        public string? Addon { get; set; }

        [StringLength(300)]
        public string? BonusInfo { get; set; }

        public ICollection<EventConfirmation> Confirmations { get; set; } = new List<EventConfirmation>();
        public ICollection<WaitingList> WaitingList { get; set; } = new List<WaitingList>();
    }
}
