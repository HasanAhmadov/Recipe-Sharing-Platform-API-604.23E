namespace Recipe_Sharing_Platform_API.DTO
{
    public class ReceiptDetails
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; } // optional image
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int LikesCount { get; set; }
    }
}