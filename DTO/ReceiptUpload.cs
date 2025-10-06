namespace Recipe_Sharing_Platform_API.DTO

{
    public class ReceiptUpload
    {
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// File to upload
        /// </summary>
        public IFormFile Image { get; set; } = default!;
    }
}