using Microsoft.AspNetCore.SignalR;

namespace Inventory.API.Hubs
{
    public class DeliveryHub : Hub
    {
        public async Task JoinBranchGroup(string branchId)
        {
            if (!string.IsNullOrEmpty(branchId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, branchId);
            }
        }
    }
}
