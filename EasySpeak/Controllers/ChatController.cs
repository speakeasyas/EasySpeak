using EasySpeak.Data;
using EasySpeak.Hubs;
using EasySpeak.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EasySpeak.Controllers
{

    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly UserManager<User> _userManager;
        public ChatController(ApplicationDbContext context,IHubContext<ChatHub> hub,UserManager<User> manager )
        {
            _context = context;
            _chatHubContext = hub;
            _userManager = manager;
        }
        // عرض صفحة الدردشة لغرفة معينة

        public async Task<IActionResult> Room(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
           
            var chatRoom = await _context.ChatRooms
           .Include(cr => cr.Messages)
           .ThenInclude(m => m.User)
           .Include(cr => cr.UserChatRooms)
           .ThenInclude(ucr => ucr.User)
           .FirstOrDefaultAsync(cr => cr.Id == id);


            if (chatRoom == null)
            {
                return NotFound();
            }

            var userChatRoom = await _context.UserChatRooms
                .FirstOrDefaultAsync(ucr => ucr.ChatRoomId == id && ucr.UserId == userId);

            if (userChatRoom == null)
            {
                return Forbid();
            }

            return View(chatRoom);
        }



        [HttpPost]

        public async Task<IActionResult> UserLeaving([FromBody] string connectionId)
        {
            await _chatHubContext.Clients.All.SendAsync("UserLeaving", connectionId);
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageDTO messageDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId); // الحصول على المستخدم من قاعدة البيانات
            var userChatRoom = await _context.UserChatRooms
                .FirstOrDefaultAsync(ucr => ucr.ChatRoomId == messageDto.RoomId && ucr.UserId == userId);

            if (userChatRoom == null)
            {
                return Forbid();
            }

            var message = new Models.Message
            {
                ChatRoomId = messageDto.RoomId,
                UserId = userId,
                Content = messageDto.MessageContent,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // إرسال اسم المستخدم مع الرسالة
            await _chatHubContext.Clients.Group(messageDto.RoomId.ToString())
                .SendAsync("ReceiveMessage", user.Name,user.ProfilePicture, messageDto.MessageContent);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendPrivateMessage(string recipientId, string messageContent)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sender = await _context.Users.FindAsync(senderId); // الحصول على المستخدم من قاعدة البيانات

            var message = new Message
            {
                Content = messageContent,
                UserId = senderId,
                RecipientId = recipientId,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            await _chatHubContext.Clients.User(recipientId).SendAsync("ReceivePrivateMessage", senderId,sender.ProfilePicture, messageContent);

            return Ok();
        }
       
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendFile(string roomId, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.FindAsync(userId);
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                var filePath = Path.Combine(uploadsFolder, file.FileName);

                try
                {
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var message = new Message
                    {
                        Content = "",
                        Timestamp = DateTime.Now,
                        UserId = user.Id,
                        ChatRoomId = int.Parse(roomId),
                        FilePath = "/uploads/" + file.FileName
                    };

                    _context.Messages.Add(message);
                    await _context.SaveChangesAsync();
                    // يمكنك حفظ مسار الملف أو أي معلومات إضافية في قاعدة البيانات هنا إذا لزم الأمر

                    // إرسال إشعار للمجموعة بأن هناك ملف تم إرساله
                    await _chatHubContext.Clients.Group(roomId).SendAsync("ReceiveFile", new { user.Name, user.ProfilePicture }, file.FileName, "/uploads/" + file.FileName);
                    return Ok();
                }
                catch (Exception ex)
                {
                    // التعامل مع الاستثناءات وتسجيل الأخطاء
                    Console.WriteLine($"Error saving file: {ex.Message}");
                    return StatusCode(500, "Internal server error");
                }
            }
            return BadRequest("No file uploaded");
        }

        [HttpPost]
        public async Task<IActionResult> SendPrivateFile(string recipientId, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sender = await _context.Users.FindAsync(senderId);
                var uploadsFolder = Path.Combine("wwwroot", "uploads");
                var filePath = Path.Combine(uploadsFolder, file.FileName);

                try
                {
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var message = new Message
                    {
                        Content = "",
                        Timestamp = DateTime.Now,
                        UserId = senderId,
                        RecipientId = recipientId,
                        FilePath = "/uploads/" + file.FileName
                    };

                    _context.Messages.Add(message);
                    await _context.SaveChangesAsync();

                    // إرسال إشعار للمستخدم المستقبل بأن هناك ملف تم إرساله
                    await _chatHubContext.Clients.User(recipientId)
                        .SendAsync("ReceivePrivateFile", sender.Name, sender.ProfilePicture, file.FileName, "/uploads/" + file.FileName);
                    await _chatHubContext.Clients.User(senderId).SendAsync("ReceivePrivateFile", sender.Name, sender.ProfilePicture, file.FileName, "/uploads/" + file.FileName);

                    return Ok();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving file: {ex.Message}");
                    return StatusCode(500, "Internal server error");
                }
            }
            return BadRequest("No file uploaded");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveRoom([FromBody] string roomId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user= _context.Users.FirstOrDefault(x=> x.Id==userId);
            var userChatRoom = await _context.UserChatRooms
                .FirstOrDefaultAsync(ucr => ucr.ChatRoomId == int.Parse(roomId) && ucr.UserId == userId);

            if (userChatRoom != null)
            {
                _context.UserChatRooms.Remove(userChatRoom);
                await _context.SaveChangesAsync();
            }

            // إبلاغ العملاء الآخرين في الغرفة بأن المستخدم غادر
            await _chatHubContext.Clients.Group(roomId).SendAsync("UserExit", user.Name);

            return Ok();
        }



    }

    public class MessageDTO
    {
        public int RoomId { get; set; }
        public string MessageContent { get; set; }
    }
   
}

