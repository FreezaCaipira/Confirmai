using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    /// <summary>
    /// One-time token generated when a player types !market in-game.
    /// The Canary Lua script calls POST /api/v1/server/generate-login-link,
    /// which creates this record and returns a signed URL.
    /// The player opens the URL in a browser and is automatically logged in
    /// (or redirected to registration) without needing to type a password.
    /// Tokens expire after 10 minutes and are single-use.
    /// </summary>
    public class GameLoginToken
    {
        public int Id { get; set; }

        /// <summary>The server that generated this token.</summary>
        public int ServerId { get; set; }
        public TibiaServer? Server { get; set; }

        /// <summary>
        /// The in-game character name as reported by the Lua script.
        /// Normalized to title-case on creation.
        /// </summary>
        [Required]
        [StringLength(120)]
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>Cryptographically random 32-byte token, stored as hex.</summary>
        [Required]
        [StringLength(64)]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>True after the token has been consumed (one-time use).</summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// UserId of the marketplace account that consumed this token.
        /// Populated on successful login/registration.
        /// </summary>
        [StringLength(450)]
        public string? ConsumedByUserId { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
    }
}
