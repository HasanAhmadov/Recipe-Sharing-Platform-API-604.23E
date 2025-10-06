using Recipe_Sharing_Platform_API.DTO;

namespace Recipe_Sharing_Platform_API.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest req);
        Task<AuthResponse> LoginAsync(LoginRequest req);
    }
}