using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class TibiaServer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do servidor e obrigatorio.")]
        [StringLength(120, ErrorMessage = "O nome do servidor deve ter ate 120 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(60, ErrorMessage = "A regiao deve ter ate 60 caracteres.")]
        public string? Region { get; set; }

        [StringLength(200, ErrorMessage = "A URL do site deve ter ate 200 caracteres.")]
        public string? WebsiteUrl { get; set; }

        [StringLength(40, ErrorMessage = "A versao do Tibia deve ter ate 40 caracteres.")]
        public string? TibiaVersion { get; set; }

        [StringLength(300, ErrorMessage = "O caminho da logo deve ter ate 300 caracteres.")]
        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [StringLength(450)]
        public string? PrimaryGameMasterUserId { get; set; }

        public ICollection<ProductServer> ProductServers { get; set; } = new List<ProductServer>();
        public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
        public ICollection<ServerRegistrationRequest> ApprovedRequests { get; set; } = new List<ServerRegistrationRequest>();
    }
}
