using EasySpeak.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EasySpeak.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }
        [ForeignKey("ChatRoom")]
        public int? ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }
        [ForeignKey("Recipient")]
        public string? RecipientId { get; set; }
        public virtual User Recipient { get; set; }
        public string? FilePath { get; set; }
    }
}
