using EasySpeak.Data;
using EasySpeak.Models;
using EasySpeak.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EasySpeak.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private static ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, List<UserChatRoom>> _rooms = new ConcurrentDictionary<string, List<UserChatRoom>>();
        public ChatHub( ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string user, string message, string roomId)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }
        
        

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var user = await _context.Users.FindAsync(userId);
            var roomId = Context.GetHttpContext().Request.Query["roomId"];
            if (user != null && !string.IsNullOrEmpty(roomId))
            {
                var room = await _context.ChatRooms.FindAsync(int.Parse(roomId));
                var userChatRoom = new UserChatRoom { User = user, ChatRoom = room, ChatRoomId = int.Parse(roomId), UserId = userId };
                _connections[Context.ConnectionId] = userId;

                if (!_rooms.ContainsKey(roomId))
                {
                    _rooms[roomId] = new List<UserChatRoom>();
                }
                if (!_rooms[roomId].Any(u => u.UserId == userId))
                {
                    _rooms[roomId].Add(userChatRoom);
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                await Clients.Group(roomId).SendAsync("UpdateUsers", _rooms[roomId]);
            }

            await base.OnConnectedAsync();
        }


        public async Task LeaveRoom(int roomId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

                var userChatRoom = await _context.UserChatRooms
                    .FirstOrDefaultAsync(ucr => ucr.ChatRoomId == roomId && ucr.UserId == userId);

                if (userChatRoom != null)
                {
                    if (_rooms.ContainsKey(roomId.ToString()))
                    {
                        _rooms[roomId.ToString()].Remove(userChatRoom);
                    }
                    _context.UserChatRooms.Remove(userChatRoom);
                    await _context.SaveChangesAsync();
                }

                // حذف اتصال المستخدم من القاموس
                _connections.TryRemove(Context.ConnectionId, out _);

                await Clients.Group(roomId.ToString()).SendAsync("UserExit", userId);

                // تحديث قائمة المستخدمين المتصلين في الغرفة
                if (_rooms.ContainsKey(roomId.ToString()))
                {
                    await Clients.Group(roomId.ToString()).SendAsync("UpdateUsers", _rooms[roomId.ToString()]);
                }
            }
            catch (Exception ex)
            {
                // سجل الخطأ في السيرفر
                Console.WriteLine($"Error in LeaveRoom: {ex.Message}");
                throw;
            }
        }
        public async Task UserLeaving(string connectionId)
        {
            Console.WriteLine("UserLeaving method executed with connectionId: " + connectionId);

            if (_connections.ContainsKey(connectionId))
            {
                var roomId = Context.GetHttpContext().Request.Query["roomId"];
                var userId = _connections[connectionId];
                if (_rooms.ContainsKey(roomId))
                {
                    var userChatRoom = _rooms[roomId].FirstOrDefault(u => u.UserId == userId);
                    if (userChatRoom != null)
                    {
                        _rooms[roomId].Remove(userChatRoom);
                    }
                }
                await Groups.RemoveFromGroupAsync(connectionId, roomId);
                _connections.TryRemove(connectionId, out _);
                
                
                // تحديث قائمة المستخدمين المتصلين في الغرفة
                await Clients.Group(roomId).SendAsync("UpdateUsers", _rooms[roomId]);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var userId))
            {
                var user = await _context.Users.FindAsync(userId);
                var roomId = Context.GetHttpContext().Request.Query["roomId"];
                if (_rooms.ContainsKey(roomId))
                {
                    var userChatRoom = _rooms[roomId].FirstOrDefault(u => u.UserId == userId);
                    if (userChatRoom != null)
                    {
                        _rooms[roomId].Remove(userChatRoom);
                    }

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync("UpdateUsers", _rooms[roomId]);
                }
               
            }

            await base.OnDisconnectedAsync(exception);
        }

        // دالة لإخطار المستخدم الآخر ببدء المحادثة الخاصة
        public async Task StartPrivateChat(string recipientId, string senderName)
        {
            await Clients.User(recipientId).SendAsync("OpenPrivateChat", Context.UserIdentifier, senderName);
            // تحميل الرسائل السابقة وإرسالها إلى المستخدم الحالي والمستقبل
            var messages = await LoadPrivateMessages(Context.UserIdentifier, recipientId);
            await Clients.Caller.SendAsync("LoadPrivateMessages", messages);
            await Clients.User(recipientId).SendAsync("LoadPrivateMessages", messages);
        }
        public async Task SendPrivateMessage(string recipientId, string message)
        {
            var senderId = Context.UserIdentifier;
            var sender = await _context.Users.FindAsync(senderId);
            var recipient = await _context.Users.FindAsync(recipientId);

            if (sender != null && recipient != null)
            {
                var msg = new Message
                {
                    Content = message,
                    UserId = senderId,
                    RecipientId = recipientId,
                    Timestamp = DateTime.UtcNow,
                    User=sender,
                    Recipient=recipient
                };

                _context.Messages.Add(msg);
                await _context.SaveChangesAsync();

                await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", sender.Name,sender.ProfilePicture, message);
                await Clients.Caller.SendAsync("ReceivePrivateMessage", sender.Name,sender.ProfilePicture, message);
                // إخطار المستخدم الآخر بفتح نافذة المحادثة الخاصة إذا كانت مغلقة
                await Clients.User(recipientId).SendAsync("OpenPrivateChat", senderId, sender.Name);
            
        }
        }
        public async Task<List<PrivateMessageViewModel>> LoadPrivateMessages(string userId, string recipientId)
        {
            var messages = await _context.Messages
                .Where(m => (m.UserId == userId && m.RecipientId == recipientId)
                         || (m.UserId == recipientId && m.RecipientId == userId))
                        //.Include(m => m.User) // تضمين بيانات المستخدم
                        //.Include(m => m.Recipient) // تضمين بيانات المستقبل
                        .OrderBy(m => m.Timestamp)
                         .Select(m => new PrivateMessageViewModel
                         {
                             Id= m.Id,
                             Content = m.Content,
                             Sname = m.User.Name, // أو m.User.Name حسب الحقل المستخدم لاسم المستخدم
                             Rname = m.Recipient.Name, // أو m.Recipient.Name حسب الحقل المستخدم لاسم المستخدم
                             Spicture=m.User.ProfilePicture,
                             Rpicture=m.Recipient.ProfilePicture,
                             FilePath=m.FilePath
                         })
                        .ToListAsync();
            
            return messages;
        }

        //public async Task SendPrivateFile(string recipientId, string filePath, string fileName)
        //{
        //    var senderId = Context.UserIdentifier;
        //    var sender = await _context.Users.FindAsync(senderId);

        //    await Clients.User(recipientId).SendAsync("ReceivePrivateFile", senderId, sender.ProfilePicture, fileName, filePath);
        //    await Clients.Caller.SendAsync("ReceivePrivateFile", senderId, sender.ProfilePicture, fileName, filePath);

        //}


    }
}
