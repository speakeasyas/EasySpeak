using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasySpeak.Models
{
    public class User:IdentityUser
    {
        
        public string Name { get; set; }
        public bool IsOnline { get; set; } = false; // إضافة الحقل

        [ValidateNever]
        public string ProfilePicture { get; set; }
        [ValidateNever]
        public virtual ICollection<Message> Messages { get; set; }
        [ValidateNever]
        public virtual ICollection<UserChatRoom> UserChatRooms { get; set; }
        [ValidateNever]
        public virtual ICollection<GroupJoinRequest> GroupJoinRequests { get; set; }

    }
}
