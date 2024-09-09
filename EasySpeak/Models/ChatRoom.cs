using EasySpeak.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasySpeak.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public bool IsPrivate {  get; set; }
        [ValidateNever]
        public virtual ICollection<Message> Messages { get; set; }
        [ValidateNever]
        public virtual ICollection<UserChatRoom> UserChatRooms { get; set; }
        [ValidateNever]
        public virtual ICollection<GroupJoinRequest> GroupJoinRequests { get; set; }

    }

}
