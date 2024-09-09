using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EasySpeak.Models
{
    public class GroupJoinRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        [ForeignKey("ChatRoom")]
        public int ChatRoomId { get; set; }
        public virtual ChatRoom ChatRoom { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public Status Status { get; set; } // Pending, Approved, Rejected

    }
}
