using Confirmai.Models;
using Confirmai.Models.Admin;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace Confirmai.Pages.Admin;

public partial class AdminUserEdit
{
    [Parameter] public string UserId { get; set; } = "";
    private ApplicationUser? user;
    private bool isLoading = true;
    private bool isSaving = false;
    [CascadingParameter] public Toast? ToastRef { get; set; }
    private EditUserModel model = new();

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        user = await UserManager.FindByIdAsync(UserId);
        if (user != null)
        {
            model = new EditUserModel
            {
                InstagramHandle = user.InstagramHandle,
                DiscordHandle = user.DiscordHandle,
                PaypalAddress = user.PaypalAddress,
                BinanceAddress = user.BinanceAddress,
                PixKey = user.PixKey,
                XHandle = user.XHandle
            };
        }
        isLoading = false;
    }

    private async Task Save()
    {
        if (user == null)
        {
            ToastRef?.Show(T["AdminUserEdit.NotFound"], "error");
            return;
        }
        isSaving = true;
        user.InstagramHandle = model.InstagramHandle;
        user.DiscordHandle = model.DiscordHandle;
        user.PaypalAddress = model.PaypalAddress;
        user.BinanceAddress = model.BinanceAddress;
        user.PixKey = model.PixKey;
        user.XHandle = model.XHandle;
        var result = await UserManager.UpdateAsync(user);
        isSaving = false;

        if (result.Succeeded)
        {
            await LogService.LogAsync(
                string.Format(T["AdminUserEdit.LogEdited"], user.Id),
                source: "Admin",
                level: "Info",
                userId: user.Id
            );
            ToastRef?.Show(T["AdminUserEdit.UpdatedToast"], "success");
            NavigationManager.NavigateTo($"/admin/users/view/{user.Id}");
        }
        else
        {
            ToastRef?.Show(T["AdminUserEdit.UpdateErrorToast"], "error");
        }
    }
}
