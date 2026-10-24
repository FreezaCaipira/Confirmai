using Confirmai.Configuration;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Confirmai.Pages;

public partial class Contact
{
    [Inject] private UiTextService T { get; set; } = default!;
    [Inject] private IOptions<AppOptions> AppOptions { get; set; } = default!;

    private string? contactEmail;

    protected override void OnInitialized()
    {
        contactEmail = AppOptions.Value.ContactEmail;
    }
}
