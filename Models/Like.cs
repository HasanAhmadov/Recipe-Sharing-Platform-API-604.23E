namespace Recipe_Sharing_Platform_API.Models
{
    public class Like
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int ReceiptId { get; set; }
        public Receipt Receipt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}