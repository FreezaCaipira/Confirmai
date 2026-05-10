using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class ForgotPasswordConfirmationModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public ForgotPasswordConfirmationModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public bool ShowDevFallbackHint { get; private set; }
        public string? LatestFallbackEmailRelativeUrl { get; private set; }
        public string? LatestFallbackEmailFileName { get; private set; }

        public void OnGet()
        {
            if (!_environment.IsDevelopment())
            {
                return;
            }

            ShowDevFallbackHint = true;

            var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? AppContext.BaseDirectory
                : _environment.WebRootPath;

            var fallbackDirectory = Path.Combine(root, "uploads", "dev-emails");
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

