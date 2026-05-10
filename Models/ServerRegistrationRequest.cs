using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class ServerRegistrationRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string RequestedName { get; set; } = string.Empty;

        [Required]
        [StringLength(40)]
        public string TibiaVersion { get; set; } = string.Empty;

        [StringLength(200)]
        public string? WebsiteUrl { get; set; }

        [StringLength(300)]
        public string? RequestedLogoPath { get; set; }

        [StringLength(1000)]
        public string? RequestNote { get; set; }

        public ServerRegistrationRequestStatus Status { get; set; } = ServerRegistrationRequestStatus.Pending;

        [StringLength(450)]
        public string? RequesterUserId { get; set; }

        public ApplicationUser? RequesterUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? ReviewerUserId { get; set; }

        public ApplicationUser? ReviewerUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(1000)]
        public string? ReviewNote { get; set; }

        public int? ApprovedServerId { get; set; }
        public TibiaServer? ApprovedServer { get; set; }
    }
}
