namespace Recipe_Sharing_Platform_API.DTO

{
    public class ReceiptUpload
    {
        public string Title { get; set; } = string.Empty;
        public IFormFile Image { get; set; } = default!;
    }
}