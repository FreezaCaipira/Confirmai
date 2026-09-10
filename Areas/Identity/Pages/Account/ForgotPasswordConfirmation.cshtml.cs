using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class ForgotPasswordConfirmationModel : PageModel
    {
        private readonly IdentityEmailSender _emailSender;

        public ForgotPasswordConfirmationModel(IdentityEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public bool ShowDevFallbackHint { get; private set; }
        public string? LatestFallbackEmailRelativeUrl { get; private set; }
        public string? LatestFallbackEmailFileName { get; private set; }

        public void OnGet()
        {
            if (!_emailSender.IsDevelopment)
            {
                return;
            }

            ShowDevFallbackHint = true;

            var fallbackDirectory = _emailSender.GetFallbackDirectory();
            if (!Directory.Exists(fallbackDirectory))
            {
                return;
            }

            var latestEmail = new DirectoryInfo(fallbackDirectory)
                .GetFiles("*.html")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestEmail == null)
            {
                return;
            }

            LatestFallbackEmailFileName = latestEmail.Name;
            LatestFallbackEmailRelativeUrl = $"/uploads/dev-emails/{latestEmail.Name}";
        }
    }
}

