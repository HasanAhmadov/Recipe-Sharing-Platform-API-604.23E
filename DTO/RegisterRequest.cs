namespace Recipe_Sharing_Platform_API.DTO

{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}