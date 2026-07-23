using Confirmai.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentProof
{
    [Parameter] public EventConfirmation? Confirmation { get; set; }
    [Parameter] public bool UploadingProof { get; set; }
    [Parameter] public string? ProofError { get; set; }
    [Parameter] public string? ProofSuccessMessage { get; set; }

    [Parameter] public EventCallback<InputFileChangeEventArgs> OnProofUpload { get; set; }

    private async Task HandleProofUpload(InputFileChangeEventArgs e)
        => await OnProofUpload.InvokeAsync(e);
}
