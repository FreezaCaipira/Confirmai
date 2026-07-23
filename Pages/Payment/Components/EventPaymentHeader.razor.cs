using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentHeader
{
    [Parameter] public Event? Event { get; set; }
    [Parameter] public bool IsFutsal { get; set; }
    [Parameter] public string? BackUrl { get; set; }
}
