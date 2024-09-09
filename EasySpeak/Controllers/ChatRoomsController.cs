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
    public class ChatRoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _chatHub;
        public ChatRoomsController(ApplicationDbContext context,IHubContext<NotificationHub> hub)
        {
            _context = context;
            _chatHub = hub;
        }
        public async Task<IActionResult> Index()
        {
            var chatRooms = await _context.ChatRooms.ToListAsync();
            return View(chatRooms);
        }
        // GET: ChatRooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ChatRooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] ChatRoom chatRoom)
        {
            if (ModelState.IsValid)
            {
                var creatorUserName = User.Identity.Name;

                var newChatRoom = new ChatRoom
                {
                    Name = chatRoom.Name
                };

                _context.ChatRooms.Add(newChatRoom);
                await _context.SaveChangesAsync();

                // إرسال إشعار لجميع المستخدمين المتصلين بغرفة الدردشة الجديدة
                await _chatHub.Clients.All.SendAsync("NewChatRoom", newChatRoom);

                return RedirectToAction(nameof(Index));
            }
            return View(chatRoom);
        }
        public async Task<IActionResult> Details(int id)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(id);
            if (chatRoom == null)
            {
                return NotFound();
            }
            return View(chatRoom);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(id);
            if (chatRoom == null)
            {
                return NotFound();
            }
            return View(chatRoom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] ChatRoom chatRoom)
        {
            if (id != chatRoom.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                _context.Update(chatRoom);
                await _context.SaveChangesAsync();
                await _chatHub.Clients.All.SendAsync("ReceiveUpdateChatRoom", chatRoom);

                return RedirectToAction(nameof(Index));
            }
            return View(chatRoom);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var chatRoom = await _context.ChatRooms.FindAsync(id);
            if (chatRoom != null)
            {
                _context.ChatRooms.Remove(chatRoom);
                await _context.SaveChangesAsync();
                await _chatHub.Clients.All.SendAsync("ReceiveDeleteChatRoom", id);

            }
            return RedirectToAction(nameof(Index));
        }
    }
}
