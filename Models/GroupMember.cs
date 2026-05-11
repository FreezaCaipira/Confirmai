using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
