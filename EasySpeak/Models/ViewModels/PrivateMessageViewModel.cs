namespace EasySpeak.Models.ViewModels
{
    public class PrivateMessageViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Sname { get; set; }
        public string Rname { get; set; }
        public string Spicture { get; set; }
        public string Rpicture { get; set; }
        public string? FilePath { get; set; }
    }
}
