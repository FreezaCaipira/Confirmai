using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public bool UseSiteIntermediary { get; set; } = true;

        public int? EstimatedDeliveryDays { get; set; }

        public Product? Product { get; set; }
        public int? OrderId { get; set; }
        public OrderModel? Order { get; set; }

        public string? SellerId { get; set; }
        public ApplicationUser? Seller { get; set; }

        /// <summary>Currency of the offer: BRL, USD or BTC</summary>
        public string? Currency { get; set; }
    }
}

