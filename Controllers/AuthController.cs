using Microsoft.AspNetCore.Mvc;
using Recipe_Sharing_Platform_API.DTO;
using Recipe_Sharing_Platform_API.Interfaces;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService auth;

        public AuthController(IAuthService _auth)
        {
            auth = _auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                var res = await auth.RegisterAsync(req);
                return Ok(res);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try
            {
                var res = await auth.LoginAsync(req);
                return Ok(res);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}