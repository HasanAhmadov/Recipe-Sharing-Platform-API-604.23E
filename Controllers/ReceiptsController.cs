using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReceiptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Upload receipt with photo
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string title)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var receipt = new Receipt
            {
                Title = title,
                Image = ms.ToArray(),
                UserId = userId
            };

            _context.Recipes.Add(receipt);
            await _context.SaveChangesAsync();
            return Ok(new { receipt.Id });
        }

        // Get all receipts (with optional search)
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            var query = _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
                query = query.Where(r => r.Title.Contains(q));

            var receipts = await query.Select(r => new
            {
                r.Id,
                r.Title,
                ImageBase64 = Convert.ToBase64String(r.Image),
                User = r.User.Username,
                Likes = r.Likes.Count
            }).ToListAsync();

            return Ok(receipts);
        }
    }

}