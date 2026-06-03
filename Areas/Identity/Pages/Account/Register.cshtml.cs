using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;

namespace Confirmai.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("auth")]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly UiTextService _t;
        private readonly LogService _log;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            UiTextService t,
            LogService log)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
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

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    MemberSince = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    // Aqui adiciona a role "user"
                    await _userManager.AddToRoleAsync(user, "user");

                    var userId = await _userManager.GetUserIdAsync(user);

                    await _log.AuditAsync(
                        AuditEvents.UserRegistered,
                        AuditEntities.User,
                        userId,
                        $"Novo usuário registrado: {user.Email}.",
                        actorUserId: userId,
                        source: AdminAuditSources.Identity,
                        metadata: new { UserId = userId, user.Email, HasPixKey = !string.IsNullOrEmpty(user.PixKey) });

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code },
                        protocol: Request.Scheme);

                    if (callbackUrl != null)
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(
                                Input.Email,
                                _t["Identity.Email.ConfirmSubject"],
                                string.Format(_t["Identity.Email.ConfirmBody"], HtmlEncoder.Default.Encode(callbackUrl), _t["Identity.Email.ConfirmAction"]));
                        }
                        catch (Exception ex)
                        {
                            await _log.LogAsync($"Falha ao enviar e-mail de confirmação para {Input.Email}: {ex.Message}", source: "Register", level: "Error");
                        }
                    }

                    if (!_signInManager.Options.SignIn.RequireConfirmedEmail)
                    {
                        // Email confirmation not required (dev mode or Email:Enabled=false in prod).
                        // Auto-confirm so the account shows as verified in the DB.
                        var confirmCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        await _userManager.ConfirmEmailAsync(user, confirmCode);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect("/grupos");
                    }

                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }
    }
}

