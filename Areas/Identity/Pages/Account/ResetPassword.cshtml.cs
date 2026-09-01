using System.ComponentModel.DataAnnotations;
using System.Text;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UiTextService _t;
        private readonly LogService? _log;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, UiTextService t, LogService? log = null)
        {
            _userManager = userManager;
            _t = t;
            _log = log;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// When true, the email field is rendered readonly (the user
            /// arrived via a link that already identifies the account).
            /// Not sent back on POST — the field is disabled.
            /// </summary>
            public bool EmailReadOnly { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Password and confirmation do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Code { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string? userId = null, string? code = null)
        {
            if (code == null)
            {
                return BadRequest(_t["Identity.Reset.ErrorCodeRequired"]);
            }

            // If userId is present (new links from ForgotPassword), resolve the
            // email from it so the user doesn't have to retype it. Old links
            // (without userId) still work — the email field stays editable.
            string? prefilledEmail = null;
            bool emailReadOnly = false;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
                if (user != null)
                {
                    prefilledEmail = user.Email;
                    emailReadOnly = true;
                }
            }

            Input = new InputModel
            {
                Email = prefilledEmail ?? string.Empty,
                EmailReadOnly = emailReadOnly,
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };

            return Page();
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
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                if (_log != null)
                    await _log.AuditAsync(
                        AuditEvents.UserPasswordReset,
                        AuditEntities.User,
                        user.Id,
                        $"Senha redefinida via link para o usu\u00e1rio {user.Email}.",
                        actorUserId: user.Id,
                        source: AdminAuditSources.Identity);

                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}


