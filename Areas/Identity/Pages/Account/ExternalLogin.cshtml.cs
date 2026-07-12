using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LogService _log;
        private readonly UiTextService _t;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            LogService log,
            UiTextService t)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _log = log;
            _t = t;
        }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return RedirectToPage("./Login", new { returnUrl });
            }

            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl })
                ?? Url.Content("~/");

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                ErrorMessage = string.Format(_t["Identity.ExternalLogin.RemoteError"], remoteError);
                return RedirectToPage("./Login", new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                ErrorMessage = _t["Identity.ExternalLogin.InfoError"];
                return RedirectToPage("./Login", new { returnUrl });
            }

            // Try existing login first.
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                await _log.AuditAsync(
                    AuditEvents.UserLoginSuccess,
                    AuditEntities.User,
                    info.ProviderKey,
                    $"Login externo bem-sucedido: {info.LoginProvider}.",
                    actorUserId: info.ProviderKey,
                    source: AdminAuditSources.Identity);

                return LocalRedirect(returnUrl);
            }

            if (signInResult.IsNotAllowed)
            {
                var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (linkedUser is not null && !linkedUser.EmailConfirmed)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(linkedUser);
                    await _userManager.ConfirmEmailAsync(linkedUser, token);
                    await _signInManager.SignInAsync(linkedUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = _t["Identity.ExternalLogin.EmailRequired"];
                return RedirectToPage("./Login", new { returnUrl });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                // Link external login to existing account.
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await _log.AuditAsync(
                        AuditEvents.UserLinkedExternalLogin,
                        AuditEntities.User,
                        user.Id,
                        $"Login externo {info.LoginProvider} vinculado ao usuário {email}.",
                        actorUserId: user.Id,
                        source: AdminAuditSources.Identity);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ErrorMessage = _t["Identity.ExternalLogin.LinkError"];
                return RedirectToPage("./Login", new { returnUrl });
            }

            // Create new account.
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                MemberSince = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ErrorMessage = _t["Identity.ExternalLogin.CreateError"];
                return RedirectToPage("./Login", new { returnUrl });
            }

            await _userManager.AddToRoleAsync(newUser, "user");
            await _userManager.AddLoginAsync(newUser, info);
            await _signInManager.SignInAsync(newUser, isPersistent: false);

            await _log.AuditAsync(
                AuditEvents.UserRegistered,
                AuditEntities.User,
                newUser.Id,
                $"Novo usuário registrado via {info.LoginProvider}: {email}.",
                actorUserId: newUser.Id,
                source: AdminAuditSources.Identity);

            return LocalRedirect(returnUrl);
        }
    }
}
