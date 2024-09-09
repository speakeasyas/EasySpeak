using EasySpeak.Data;
using EasySpeak.Hubs;
using EasySpeak.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasySpeak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, IHubContext<ChatHub> chatHubContext, IHubContext<NotificationHub> notificationHubContext)
        {
            _context = context;
            _userManager = userManager;
            _chatHubContext = chatHubContext;
            _notificationHubContext = notificationHubContext;
        }
        
        
        // GET: /Index
        public IActionResult Index()
        {
            // Get counts
            var totalUsers = _context.Users.Count();
            var totalRooms = _context.ChatRooms.Count();
            var pendingRequests = _context.GroupJoinRequests.Where(r => r.Status==Status.Pending).Count();

            // Pass data to the view
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalRooms"] = totalRooms;
            ViewData["PendingRequests"] = pendingRequests;

            return View();
        }

        

       
    }
}
