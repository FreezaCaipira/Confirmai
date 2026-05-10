using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Confirmai.Services;

namespace Confirmai.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("auth")]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly UiTextService _t;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, UiTextService t)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _t = t;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code },
                protocol: Request.Scheme);

            if (callbackUrl != null)
            {
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    _t["Identity.Email.ResetSubject"],
                    string.Format(_t["Identity.Email.ResetBody"], HtmlEncoder.Default.Encode(callbackUrl), _t["Identity.Email.ResetAction"]));
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}


