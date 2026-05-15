using System.ComponentModel.DataAnnotations;
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

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<MatchSchedule> Schedules { get; set; } = new List<MatchSchedule>();
    }
}
