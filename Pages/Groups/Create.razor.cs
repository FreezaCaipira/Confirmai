using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Create
{
    private sealed class CreateGroupForm
    {
        [Required(ErrorMessage = "Informe o nome do grupo.")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string Name { get; set; } = string.Empty;

        public Sport Sport { get; set; } = Sport.Futsal;

        [Required(ErrorMessage = "Informe a cidade.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione a UF.")]
        public string StateCode { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = true;
    }

    private CreateGroupForm form      = new();
    private bool            isSaving  = false;
    private string          saveError = string.Empty;

    private Dictionary<string, string[]> citiesByState = new();

    private IBrowserFile?   logoFile = null;
    private string?         logoPreview = null;
    private string          logoFeedback = string.Empty;
    private bool            logoError = false;

    private static readonly string[] BrazilianStates =
        ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
         "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private IWebHostEnvironment Env { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var path = Path.Combine(Env.WebRootPath, "data", "cities.json");
            Console.WriteLine($"[CreateGroup] Loading cities from: {path}");
            var json = await File.ReadAllTextAsync(path);
            citiesByState = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json) ?? new();
            Console.WriteLine($"[CreateGroup] Loaded {citiesByState.Count} states, SP has {citiesByState.GetValueOrDefault("SP")?.Length ?? 0} cities");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateGroup] Failed to load cities.json: {ex.Message}");
            citiesByState = new();
        }
    }

    private void OnStateChanged()
    {
        form.City = string.Empty;
    }

    private async Task Save()
    {
        isSaving  = true;
        saveError = string.Empty;

        try
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                saveError = "Você precisa estar autenticado.";
                isSaving  = false;
                return;
            }

            await using var db = await DbFactory.CreateDbContextAsync();

            var group = new Group
            {
                Name            = form.Name.Trim(),
                Sport           = form.Sport,
                City            = form.City.Trim(),
                StateCode       = form.StateCode.Trim().ToUpperInvariant(),
                IsActive        = true,
                IsPrivate       = true,
                CreatedByUserId = userId,
                InviteCode      = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                LogoPath        = null,
            };
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            // Salvar logo se fornecida
            if (logoFile is not null)
            {
                try
                {
                    var logosDir = Path.Combine(Env.WebRootPath, "uploads", "groups");
                    Directory.CreateDirectory(logosDir);

                    var ext = Path.GetExtension(logoFile.Name).ToLowerInvariant();
                    var filename = $"{Guid.NewGuid()}{ext}";
                    var fullPath = Path.Combine(logosDir, filename);

                    await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                    await using var readStream = logoFile.OpenReadStream(5 * 1024 * 1024);
                    await readStream.CopyToAsync(stream);

                    group.LogoPath = $"/uploads/groups/{filename}";
                    db.Groups.Update(group);
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Se falhar ao salvar logo, continua sem ela
                }
            }

            db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId  = userId,
                Role    = GroupMemberRole.Admin,
            });
            await db.SaveChangesAsync();

            await LogService.AuditAsync(
                AuditEvents.GroupCreated,
                AuditEntities.Group,
                group.Id.ToString(),
                $"Grupo criado: \"{group.Name}\" (sport: {group.Sport})",
                userId, "GroupCreate");

            NavigationManager.NavigateTo($"/grupo/{group.Id}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao criar grupo: {ex.Message}";
            isSaving  = false;
        }
    }

    private async Task OnLogoSelected(InputFileChangeEventArgs e)
    {
        logoFile = e.File;
        logoError = false;
        logoFeedback = string.Empty;

        const long maxBytes = 5 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        var ext = Path.GetExtension(logoFile.Name).ToLowerInvariant();
        if (!allowedExts.Contains(ext) || !allowedTypes.Contains(logoFile.ContentType))
        {
            logoError = true;
            logoFeedback = "Formato inválido. Use PNG, JPG ou WebP.";
            logoFile = null;
            return;
        }

        if (logoFile.Size > maxBytes)
        {
            logoError = true;
            logoFeedback = "Arquivo muito grande. Máximo 5 MB.";
            logoFile = null;
            return;
        }

        try
        {
            var buffer = new byte[logoFile.Size];
            await using var stream = logoFile.OpenReadStream(maxBytes);
            await stream.ReadExactlyAsync(buffer);
            logoPreview = $"data:{logoFile.ContentType};base64,{Convert.ToBase64String(buffer)}";
            logoFeedback = "Imagem selecionada com sucesso!";
            logoError = false;
        }
        catch
        {
            logoError = true;
            logoFeedback = "Erro ao processar imagem.";
            logoFile = null;
        }
    }
}
