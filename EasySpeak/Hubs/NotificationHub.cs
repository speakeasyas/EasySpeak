using EasySpeak.Models;
using Microsoft.AspNetCore.SignalR;

namespace EasySpeak.Hubs
{
    public class NotificationHub:Hub
    {
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
        public async Task LogoutUser(string userId)
        {
            await Clients.User(userId).SendAsync("Logout");
        }
        public async Task UpdateChatRoom(ChatRoom updatedRoom)
        {
            await Clients.All.SendAsync("ReceiveUpdateChatRoom", updatedRoom);
        }
        
        public async Task DeleteChatRoom(int roomId)
        {
            await Clients.All.SendAsync("ReceiveDeleteChatRoom", roomId);
        }
        
    }
}
