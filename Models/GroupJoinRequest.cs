using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class GroupJoinRequest
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;

        public DateTime? RespondedAt { get; set; }

        [StringLength(450)]
        public string? RespondedByUserId { get; set; }
    }
}
