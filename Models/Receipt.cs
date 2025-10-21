namespace Recipe_Sharing_Platform_API.Models

{
    public class Receipt
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public byte[] Image { get; set; }  // store photo in DB
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Like> Likes { get; set; }
    }
}