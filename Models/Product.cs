using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres.")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O preço deve ser maior ou igual a zero.")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O peso deve ser maior ou igual a zero.")]
        public decimal Weight { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Atk inválido.")]
        public int Attack { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Def inválido.")]
        public int Defense { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Arm inválido.")]
        public int Armor { get; set; }

        [StringLength(800, ErrorMessage = "Drops deve ter até 800 caracteres.")]
        public string? DropSources { get; set; }

        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public string? ShortDescription { get; set; }
        public string? Category { get; set; }
        [StringLength(64, ErrorMessage = "Gateway de precificacao deve ter ate 64 caracteres.")]
        public string? PricingGateway { get; set; }
        [StringLength(9, ErrorMessage = "A cor deve ter ate 9 caracteres.")]
        public string? AccentColor { get; set; }
        public bool RequiresDelivery { get; set; } = false;
    }
}

