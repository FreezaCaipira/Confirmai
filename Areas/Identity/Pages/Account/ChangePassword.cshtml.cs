using System.ComponentModel.DataAnnotations;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Confirmai.Areas.Identity.Pages.Account
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly LogService? _log;

        public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, LogService? log = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _log = log;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);

            if (_log != null)
                await _log.AuditAsync(
                    AuditEvents.UserPasswordChanged,
                    AuditEntities.User,
                    user.Id,
                    "Usu\u00e1rio alterou a pr\u00f3pria senha.",
                    actorUserId: user.Id,
                    source: AdminAuditSources.Identity);

            return RedirectToPage("./ChangePasswordConfirmation");
        }
    }
}


