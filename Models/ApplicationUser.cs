using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }

        [StringLength(120)]
        public string? InstagramHandle { get; set; }

        [StringLength(120)]
        public string? DiscordHandle { get; set; }

        [StringLength(160)]
        public string? PaypalAddress { get; set; }

        [StringLength(160)]
        public string? BinanceAddress { get; set; }

        [StringLength(160)]
        public string? PixKey { get; set; }

        [StringLength(120)]
        public string? XHandle { get; set; }

        /// <summary>WhatsApp number with country code (e.g. +5511999999999)</summary>
        [StringLength(20)]
        public string? WhatsAppNumber { get; set; }

        /// <summary>Whether user has opted in to receive WhatsApp notifications</summary>
        public bool WhatsAppOptIn { get; set; } = false;

        /// <summary>Relative path to uploaded avatar, e.g. /uploads/avatars/{guid}.png</summary>
        [StringLength(260)]
        public string? AvatarPath { get; set; }

        /// <summary>UTC timestamp when the account was registered.</summary>
        public DateTime? MemberSince { get; set; }
    }
}

