using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class PaymentRecord
    {
        public int Id { get; set; }
        public int? ServerId { get; set; }
        public int ProductId { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Address { get; set; } = "";
        public string? PaymentId { get; set; } 
        public string? PaymentMethod { get; set; } 
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public string? PrivateKey { get; set; }

        public Product? Product { get; set; }

        public string? SellerId { get; set; }
        public ApplicationUser? Seller { get; set; }

        /// <summary>Currency of the offer: BRL, USD or BTC</summary>
        public string? Currency { get; set; }

        // ── Taxa de Serviço / Repasse ──────────────────────────────────────

        /// <summary>Valor base sem taxa (para repasse ao organizador).</summary>
        public decimal? BaseAmount { get; set; }

        /// <summary>Taxa de serviço retida pela plataforma.</summary>
        public decimal? FeeAmount { get; set; }

        /// <summary>Estado do repasse ao organizador.</summary>
        public PayoutStatus? PayoutStatus { get; set; }

        /// <summary>EndToEndId do Pix de repasse (identificador único do banco).</summary>
        [StringLength(140)]
        public string? PayoutEndToEndId { get; set; }

        /// <summary>Chave PIX do beneficiário (organizador).</summary>
        [StringLength(140)]
        public string? PayoutPixKey { get; set; }

        /// <summary>Quando o repasse foi enviado.</summary>
        public DateTime? PayoutSentAt { get; set; }

        /// <summary>Quando o repasse foi confirmado pelo banco.</summary>
        public DateTime? PayoutConfirmedAt { get; set; }

        /// <summary>Mensagem de erro se o repasse falhou.</summary>
        [StringLength(500)]
        public string? PayoutErrorMessage { get; set; }

        /// <summary>Número de tentativas de repasse (para retry).</summary>
        public int PayoutRetryCount { get; set; } = 0;
    }
}

