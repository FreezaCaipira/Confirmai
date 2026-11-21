using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Confirmai.Models;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        // C46 (1.0e): GET never signs out — it only renders the confirmation
        // page (a GET endpoint mutating auth is a CSRF/prefetch footgun).
        // The actual logout is the POST below with antiforgery validation.
        public IActionResult OnGet(string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                ReturnUrl = returnUrl;
            }
            return Page();
        }

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return Redirect("~/");
        }
    }
}

