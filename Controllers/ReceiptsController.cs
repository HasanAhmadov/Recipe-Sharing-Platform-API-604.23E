using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.DTO;
using Recipe_Sharing_Platform_API.Interfaces;
using Recipe_Sharing_Platform_API.Models;
using System.Security.Claims;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public ReceiptsController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] ReceiptUpload dto)
        {
            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("No file uploaded.");

            // ✅ Get current user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Invalid or missing token.");

            int userId = int.Parse(userIdClaim.Value);

            using var ms = new MemoryStream();
            await dto.Image.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            // ✅ Associate receipt with user
            var receipt = new Receipt
            {
                Title = dto.Title,
                Image = fileBytes,
                UserId = userId
            };

            _context.Recipes.Add(receipt); // if your DbSet is called "Recipes"
            await _context.SaveChangesAsync();

            return Ok(new { message = "Uploaded successfully", receipt.Title, UserId = userId });
        }

        [AllowAnonymous]
        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetImage(int id)
        {
            var receipt = await _context.Recipes.FindAsync(id);
            if (receipt == null || receipt.Image == null)
                return NotFound();

            // Return image bytes with correct MIME type
            return File(receipt.Image, "image/jpeg");
        }


    }
}