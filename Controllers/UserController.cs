using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recipe_Sharing_Platform_API.Data;
using System.Security.Claims;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context) { _context = context; }

        [HttpGet("GetUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(new { user.Id, user.Username, user.Name });
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users.Select(u => new { u.Id, u.Username, u.Name }).ToListAsync();
            return Ok(users);
        }

        [HttpGet("GetUserFromSession")]
        public async Task<IActionResult> GetUserFromSession() {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound( new { message = "User not found" });

            return Ok(new { user.Id, user.Username, user.Name });
        }
    }
}