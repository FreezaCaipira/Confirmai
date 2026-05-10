using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Confirmai.Hubs
{
    [Authorize]
    public class PaymentHub : Hub { }
}

