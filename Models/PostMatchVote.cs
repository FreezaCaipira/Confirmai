using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    /// <summary>Voto de destaque pós-partida — um voto por jogador por evento.</summary>
    public class PostMatchVote
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        /// <summary>Quem votou.</summary>
        [StringLength(450)]
        public string VoterUserId { get; set; } = string.Empty;
        public ApplicationUser Voter { get; set; } = null!;

        /// <summary>Em quem votou.</summary>
        [StringLength(450)]
        public string VotedForUserId { get; set; } = string.Empty;
        public ApplicationUser VotedFor { get; set; } = null!;

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}
