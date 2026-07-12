using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Confirmai.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("auth")]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminSecurityPolicyService _securityPolicyService;
        private readonly UiTextService _t;
        private readonly LogService _log;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AdminSecurityPolicyService securityPolicyService,
            UiTextService t,
            LogService log)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _securityPolicyService = securityPolicyService;
            _t = t;
            _log = log;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var policy = await _securityPolicyService.GetRuntimePolicyAsync();
                var user = await FindByLoginIdentifierAsync(Input.Email);

                if (user is null)
                {
                    ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorInvalid"]);
                    return Page();
                }

                if (await _userManager.IsLockedOutAsync(user))
                {
                    ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorLocked"]);
                    return Page();
                }

                if (policy.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorConfirmEmail"]);
                    return Page();
                }

                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    await RegisterFailedAttemptAsync(user, policy);
                    return Page();
                }

                await _userManager.ResetAccessFailedCountAsync(user);
                await _signInManager.SignInAsync(user, Input.RememberMe);

                await _log.AuditAsync(
                    AuditEvents.UserLoginSuccess,
                    AuditEntities.User,
                    user.Id,
                    $"Login bem-sucedido: {user.UserName}.",
                    actorUserId: user.Id,
                    source: AdminAuditSources.Identity,
                    metadata: new { user.Id, user.UserName, Input.RememberMe });

                var target = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
                        ? ReturnUrl
                        : "/";
                return LocalRedirect(target);

            }
            return Page();
        }

        private async Task<ApplicationUser?> FindByLoginIdentifierAsync(string email)
        {
            var user = await _userManager.FindByNameAsync(email);
            if (user is not null)
            {
                return user;
            }

            return await _userManager.FindByEmailAsync(email);
        }

        private async Task RegisterFailedAttemptAsync(ApplicationUser user, RuntimeSecurityPolicy policy)
        {
            if (!user.LockoutEnabled)
            {
                ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorInvalid"]);
                return;
            }

            await _userManager.AccessFailedAsync(user);
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);

            if (failedCount >= policy.LockoutMaxFailedAccessAttempts)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(policy.LockoutMinutes));
                await _userManager.ResetAccessFailedCountAsync(user);

                await _log.AuditAsync(
                    AuditEvents.UserLockedOut,
                    AuditEntities.User,
                    user.Id,
                    $"Usuário bloqueado por excesso de tentativas: {user.UserName}.",
                    actorUserId: user.Id,
                    source: AdminAuditSources.Identity,
                    level: "Warning",
                    metadata: new { user.Id, user.UserName, FailedCount = failedCount, policy.LockoutMinutes });

                ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorLocked"]);
                return;
            }

            await _log.AuditAsync(
                AuditEvents.UserLoginFailed,
                AuditEntities.User,
                user.Id,
                $"Tentativa de login inválida: {user.UserName}.",
                actorUserId: user.Id,
                source: AdminAuditSources.Identity,
                level: "Warning",
                metadata: new { user.Id, user.UserName, FailedCount = failedCount });

            ModelState.AddModelError(string.Empty, _t["Identity.Login.ErrorInvalid"]);
        }
    }
}

