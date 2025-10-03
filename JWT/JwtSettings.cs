namespace Recipe_Sharing_Platform_API.JWT
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;       
        public string Issuer { get; set; } = "recipesharingplatform";
        public string Audience { get; set; } = "recipesharingplatform";
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 30;
    }
}