namespace EasySpeak.Models.ViewModels
{
    public class ChatRoomViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MembersCount { get; set; }
        public int OnlineMembersCount { get; set; }
        public bool UserIsMember { get; set; }
    }

}
