using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models.Admin
{
    public class EditUserModel
    {
        [Display(Name = "Instagram")]
        [StringLength(120)]
        public string? InstagramHandle { get; set; }

        [Display(Name = "Discord")]
        [StringLength(120)]
        public string? DiscordHandle { get; set; }

        [Display(Name = "PayPal")]
        [StringLength(160)]
        public string? PaypalAddress { get; set; }

        [Display(Name = "Binance")]
        [StringLength(160)]
        public string? BinanceAddress { get; set; }

        [Display(Name = "PIX")]
        [StringLength(160)]
        public string? PixKey { get; set; }

        [Display(Name = "X")]
        [StringLength(120)]
        public string? XHandle { get; set; }
    }
}

