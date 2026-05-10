using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Pages.Auth
{
    public class GameLoginModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GameLoginModel(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // View state
        public string Token { get; private set; } = string.Empty;
        public string PlayerName { get; private set; } = string.Empty;
        public string ServerName { get; private set; } = string.Empty;
        public bool IsExpired { get; private set; }
        public bool IsInvalid { get; private set; }
        public bool IsSuccess { get; private set; }

        public bool HasExistingAccount { get; private set; }
        public bool ForceRegister { get; private set; }
        public string? ErrorMessage { get; private set; }

        public async Task<IActionResult> OnGetAsync(string? token, bool forceRegister = false)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                IsInvalid = true;
                return Page();
            }

            Token = token;
            var record = await LoadTokenAsync(token);

            if (record == null)
            {
                IsInvalid = true;
                return Page();
            }

            if (record.IsUsed || record.ExpiresAtUtc < DateTime.UtcNow)
            {
                IsExpired = true;
                return Page();
            }

            PlayerName = record.PlayerName;
            ServerName = record.Server?.Name ?? string.Empty;

            // If a session is active, invalidate it across all tabs/devices
            // before proceeding, so the previous account cannot remain active.
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    // Rotating the security stamp invalidates every cookie/session
                    // already issued for this user — including other open tabs.
                    await _userManager.UpdateSecurityStampAsync(currentUser);
                }
                await _signInManager.SignOutAsync();
                return Redirect($"/auth/game-login?token={token}");
            }

            // Check if there is already a ServerMember with this playerName on this server
            var existingMember = await _db.ServerMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.ServerId == record.ServerId &&
                    m.InGamePlayerName == record.PlayerName);

            HasExistingAccount = existingMember != null && !forceRegister;
            ForceRegister = forceRegister;
            return Page();
        }

        // Already logged in — just link the character
        public async Task<IActionResult> OnPostLinkAndContinueAsync(string token)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage(new { token });

            var record = await LoadTokenAsync(token);
            if (record == null || record.IsUsed || record.ExpiresAtUtc < DateTime.UtcNow)
            {
                Token = token;
                IsExpired = true;
                return Page();
            }

            PlayerName = record.PlayerName;
            ServerName = record.Server?.Name ?? string.Empty;
            var userId = _userManager.GetUserId(User)!;
            var linkError = await LinkCharacterAsync(record, userId);
            if (linkError != null)
            {
                ErrorMessage = linkError;
                return Page();
            }
            IsSuccess = true;
            return Page();
        }

        // Login with existing account, then link
        public async Task<IActionResult> OnPostLoginAndLinkAsync(string token, string email, string password)
        {
            Token = token;
            var record = await LoadTokenAsync(token);

            if (record == null || record.IsUsed || record.ExpiresAtUtc < DateTime.UtcNow)
            {
                IsExpired = true;
                PlayerName = record?.PlayerName ?? string.Empty;
                return Page();
            }

            PlayerName = record.PlayerName;
            ServerName = record.Server?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "E-mail e senha são obrigatórios.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                ErrorMessage = "E-mail ou senha incorretos.";
                HasExistingAccount = true;
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
            var loginLinkError = await LinkCharacterAsync(record, user.Id);
            if (loginLinkError != null)
            {
                ErrorMessage = loginLinkError;
                HasExistingAccount = true;
                return Page();
            }
            IsSuccess = true;
            return Page();
        }

        // Register a new account, then link
        public async Task<IActionResult> OnPostRegisterAsync(string token, string email, string password, string confirmPassword)
        {
            Token = token;
            var record = await LoadTokenAsync(token);

            if (record == null || record.IsUsed || record.ExpiresAtUtc < DateTime.UtcNow)
            {
                IsExpired = true;
                PlayerName = record?.PlayerName ?? string.Empty;
                return Page();
            }

            PlayerName = record.PlayerName;
            ServerName = record.Server?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "E-mail e senha são obrigatórios.";
                return Page();
            }

            if (password != confirmPassword)
            {
                ErrorMessage = "As senhas não coincidem.";
                return Page();
            }

            // Check if email is already registered
            var existing = await _userManager.FindByEmailAsync(email.Trim());
            if (existing != null)
            {
                ErrorMessage = "Este e-mail já está cadastrado. Faça login para vincular seu personagem.";
                HasExistingAccount = true;
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = email.Trim(),
                Email = email.Trim(),
                EmailConfirmed = true  // auto-confirm for game-login registrations
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
                return Page();
            }

            await _userManager.AddToRoleAsync(user, "user");
            await _signInManager.SignInAsync(user, isPersistent: true);
            var registerLinkError = await LinkCharacterAsync(record, user.Id);
            if (registerLinkError != null)
            {
                ErrorMessage = registerLinkError;
                return Page();
            }
            IsSuccess = true;
            return Page();
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private async Task<GameLoginToken?> LoadTokenAsync(string token)
        {
            return await _db.GameLoginTokens
                .Include(t => t.Server)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        private async Task<string?> LinkCharacterAsync(GameLoginToken record, string userId)
        {
            // Block if this character is already claimed by a different user
            var claimedByOther = await _db.ServerMembers
                .AsNoTracking()
                .AnyAsync(m => m.ServerId == record.ServerId
                            && m.InGamePlayerName == record.PlayerName
                            && m.UserId != userId);

            if (claimedByOther)
                return "Este personagem já está vinculado a outra conta. O vínculo é permanente e não pode ser transferido.";

            // Find current user's member for this server
            var member = await _db.ServerMembers
                .FirstOrDefaultAsync(m => m.ServerId == record.ServerId && m.UserId == userId);

            // Block if this user already has a DIFFERENT character linked (immutability)
            if (member?.InGamePlayerName != null && member.InGamePlayerName != record.PlayerName)
                return $"Você já possui o personagem \"{member.InGamePlayerName}\" vinculado neste servidor. O vínculo não pode ser alterado.";

            // Mark token as used
            record.IsUsed = true;
            record.ConsumedByUserId = userId;
            record.ConsumedAtUtc = DateTime.UtcNow;

            if (member == null)
            {
                member = new ServerMember
                {
                    ServerId = record.ServerId,
                    UserId = userId,
                    Role = ServerMemberRole.User,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ServerMembers.Add(member);
            }

            member.InGamePlayerName = record.PlayerName;

            await _db.SaveChangesAsync();
            return null;
        }
    }
}
