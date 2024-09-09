using EasySpeak.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EasySpeak.Models
{
    public class UserChatRoom
    {
        [Key, Column(Order = 0)]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        [Key, Column(Order = 1)]
        [ForeignKey("ChatRoom")]
        public int ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }
    }
}
