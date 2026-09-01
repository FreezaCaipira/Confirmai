using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly UiTextService _t;
        private readonly EmailTemplateService _emailTemplate;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, UiTextService t, EmailTemplateService emailTemplate)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _t = t;
            _emailTemplate = emailTemplate;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

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
                StatusMessage = _t["Identity.Resend.StatusQueued"];
                return RedirectToPage();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);

            if (callbackUrl != null)
            {
                var subject = _t["Identity.Email.ConfirmSubject"];
                var ctaText = _t["Identity.Email.ConfirmAction"];
                var paragraphs = new[] { string.Format(_t["Identity.Email.ConfirmBody"], HtmlEncoder.Default.Encode(callbackUrl), ctaText) };
                var html = _emailTemplate.RenderHtml(subject, subject, paragraphs, ctaText, callbackUrl);
                var text = _emailTemplate.RenderText(subject, paragraphs, ctaText, callbackUrl);

                if (_emailSender is IdentityEmailSender typedSender)
                    await typedSender.SendEmailAsync(Input.Email, subject, html, text);
                else
                    await _emailSender.SendEmailAsync(Input.Email, subject, html);
            }

            StatusMessage = _t["Identity.Resend.StatusQueued"];
            return RedirectToPage();
        }
    }
}


