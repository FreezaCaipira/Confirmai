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

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
