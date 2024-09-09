using EasySpeak.Data;
using EasySpeak.Hubs;
using EasySpeak.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasySpeak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GroupJoinRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public GroupJoinRequestsController(ApplicationDbContext context,IHubContext<NotificationHub> hub)
        {
            _context = context;
            _notificationHub = hub;
        }
        public async Task<IActionResult> Index()
        {
            var joinRequests = await _context.GroupJoinRequests
                .Include(jr => jr.User)
                .Include(jr => jr.ChatRoom)
                .Where(jr => jr.Status == Status.Pending)
                .ToListAsync();
            return View(joinRequests);
        }

        public async Task<IActionResult> Approve(int id)
        {
            var joinRequest = await _context.GroupJoinRequests.FindAsync(id);
            if (joinRequest != null)
            {
                joinRequest.Status = Status.Approved;
                _context.Update(joinRequest);

                var userChatRoom = new UserChatRoom
                {
                    UserId = joinRequest.UserId,
                    ChatRoomId = joinRequest.ChatRoomId
                };
                _context.UserChatRooms.Add(userChatRoom);

                await _context.SaveChangesAsync();
                // إرسال إشعار إلى المستخدم بقبول الطلب
                //await _notificationHub.Clients.User(joinRequest.UserId).SendAsync("ReceiveNotification", $"Your request to join {joinRequest.ChatRoom.Name} has been approved.");
                await _notificationHub.Clients.User(joinRequest.UserId).SendAsync("ReceiveNotification", $"Your request to join  has been approved.");
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Reject(int id)
        {
            var joinRequest = await _context.GroupJoinRequests.FindAsync(id);
            if (joinRequest != null)
            {
                joinRequest.Status = Status.Rejected;
                _context.Update(joinRequest);
                await _context.SaveChangesAsync();
                // إرسال إشعار إلى المستخدم برفض الطلب
                await _notificationHub.Clients.User(joinRequest.UserId).SendAsync("ReceiveNotification", $"Your request to join {joinRequest.ChatRoom.Name} has been rejected.");
            }
            return RedirectToAction(nameof(Index));
        }
        
    }
}
