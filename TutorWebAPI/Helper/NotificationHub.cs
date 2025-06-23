using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace TutorWebAPI.Helper
{
    public class NotificationHub : Hub
    {
        public async Task SendNotificationToUsers(string message, List<int> userIds)
        {
            foreach (var userId in userIds)
            {

                await Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", message);
            }
        }


        public override async Task OnConnectedAsync()
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId != 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
                Console.WriteLine($"User {userId} connected.");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId != 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
                Console.WriteLine($"User {userId} disconnected.");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
