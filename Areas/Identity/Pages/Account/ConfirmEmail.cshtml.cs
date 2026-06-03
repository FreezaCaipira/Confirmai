using System.Text;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;

namespace Confirmai.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UiTextService _t;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, UiTextService t)
        {
            _userManager = userManager;
            _t = t;
        }

        public string StatusMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            if (userId == null || code == null)
            {
                StatusMessage = _t["Identity.ConfirmEmail.InvalidLink"];
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = _t["Identity.Common.UserNotFound"];
                return Page();
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded
                ? _t["Identity.ConfirmEmail.Success"]
                : _t["Identity.ConfirmEmail.Failure"];

            return Page();
        }
    }
}


