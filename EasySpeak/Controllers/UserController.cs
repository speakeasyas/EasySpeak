using EasySpeak.Data;
using EasySpeak.Hubs;
using EasySpeak.Models;
using EasySpeak.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EasySpeak.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly IHubContext<NotificationHub> _chatHub;
        private readonly UserManager<User> _userManager; // إضافة إدارة المستخدمين
        private readonly ApplicationDbContext _context;
        public UserController(ILogger<UserController> logger, IHubContext<NotificationHub> chatHub,UserManager<User> userManager,ApplicationDbContext dbContext)
        {
            _logger = logger;
            _chatHub = chatHub;
            _userManager = userManager;
            _context = dbContext;
        }
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming user is authenticated
            var userChatRoomIds = await _context.UserChatRooms
                .Where(ucr => ucr.UserId == userId)
                .Select(ucr => ucr.ChatRoomId)
                .ToListAsync();

            var chatRooms = await _context.ChatRooms
                .Select(cr => new {
                    cr.Id,
                    cr.Name,
                    MembersCount = cr.UserChatRooms.Count,
                    OnlineMembersCount = cr.UserChatRooms.Count(ucr => ucr.User.IsOnline),
                    UserIsMember = userChatRoomIds.Contains(cr.Id) // Check if user is a member of this chat room
                }).ToListAsync();
            List<ChatRoomViewModel> chatRoomList = new List<ChatRoomViewModel>();
            foreach (var item in chatRooms)
            {
                ChatRoomViewModel vm = new ChatRoomViewModel()
                {
                    Id = item.Id,
                    Name = item.Name,
                    MembersCount = item.MembersCount,
                    OnlineMembersCount = item.OnlineMembersCount,
                    UserIsMember = item.UserIsMember
                };
                chatRoomList.Add(vm);
            }
            ViewData["ChatRooms"] = chatRoomList;
            ViewData["User"] = await _context.Users.FindAsync(userId); // Assuming you have a User entity
            ViewData["UserEmail"] = User.Identity.Name; // Assuming the email is the user name

            return View();




            
        }
        [HttpPost]
        public async Task<IActionResult> RequestJoinGroup(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingRequest = await _context.GroupJoinRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ChatRoomId == groupId && r.Status == Status.Pending);
            if (existingRequest != null)
            {
                return BadRequest("You have already sent a join request for this group.");
            }
           
                var joinRequest = new GroupJoinRequest
                {
                    UserId = userId,
                    ChatRoomId = groupId,
                    Status = Status.Pending,
                    RequestDate = DateTime.UtcNow
                };

                _context.GroupJoinRequests.Add(joinRequest);
                await _context.SaveChangesAsync();
                return Ok();
            
        }
        [HttpGet]
        public async Task<IActionResult> CheckPendingJoinRequests(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasPendingRequest = await _context.GroupJoinRequests
                .AnyAsync(gr => gr.UserId == userId && gr.ChatRoomId == groupId && gr.Status == Status.Pending);

            return Json(new { hasPendingRequest });
        }
        public async Task<IActionResult> RequestsDetails()
        {
            var currentUserId = _userManager.GetUserId(User);
            var requests = await _context.GroupJoinRequests
                .Include(r => r.User)
                .Include(r => r.ChatRoom)
                .Where(r => r.UserId == currentUserId)
                .ToListAsync();

            return View(requests);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.GroupJoinRequests.FindAsync(id);
            if (request != null)
            {
                _context.GroupJoinRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(RequestsDetails));
        }


    }
}

