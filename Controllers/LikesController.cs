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
    public class LikesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("{receiptId}")]
        public async Task<IActionResult> Like(int receiptId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ReceiptId == receiptId);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { liked = false });
            }

            var like = new Like
            {
                UserId = userId,
                ReceiptId = receiptId
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return Ok(new { liked = true });
        }
    }
}