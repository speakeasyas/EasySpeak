using Microsoft.EntityFrameworkCore;
using EasySpeak.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Reflection.Emit;

namespace EasySpeak.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<UserChatRoom> UserChatRooms { get; set; }
        public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
        public DbSet<User> Users {  get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserChatRoom>()
                .HasKey(uc => new { uc.UserId, uc.ChatRoomId });

            builder.Entity<UserChatRoom>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserChatRooms)
                .HasForeignKey(uc => uc.UserId);

            builder.Entity<UserChatRoom>()
                .HasOne(uc => uc.ChatRoom)
                .WithMany(cr => cr.UserChatRooms)
                .HasForeignKey(uc => uc.ChatRoomId);

            builder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.ChatRoom)
                .WithMany(cr => cr.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupJoinRequest>()
                .HasOne(gjr => gjr.User)
                .WithMany(u => u.GroupJoinRequests)
                .HasForeignKey(gjr => gjr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupJoinRequest>()
                .HasOne(gjr => gjr.ChatRoom)
                .WithMany(cr => cr.GroupJoinRequests)
                .HasForeignKey(gjr => gjr.ChatRoomId)
                .OnDelete(DeleteBehavior.Restrict);
            //---------------------
            
        }
    }
}
