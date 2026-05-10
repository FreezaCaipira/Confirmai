using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class ServerMember
    {
        public int Id { get; set; }
        public int ServerId { get; set; }
        public TibiaServer Server { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public ServerMemberRole Role { get; set; } = ServerMemberRole.User;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The player's in-game character name on this server.
        /// Set automatically via the !market game command.
        /// </summary>
        [StringLength(120)]
        public string? InGamePlayerName { get; set; }

        /// <summary>
        /// Number of times a delivery failed because the seller did not have
        /// the item in their Store Inbox. Used as a visible reputation indicator.
        /// </summary>
        public int FailedDeliveryCount { get; set; }
    }
}
