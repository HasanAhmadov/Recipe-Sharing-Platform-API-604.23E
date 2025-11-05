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
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("SearchReceipts")]
        public async Task<IActionResult> SearchReceipts([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(new List<object>());

            var query = q.ToLower();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var searchResults = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Where(r =>
                    r.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    (r.User != null && r.User.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                )
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    UserName = r.User.Username,
                    LikesCount = r.Likes.Count,
                    LikedByCurrentUser = userId != 0 && r.Likes.Any(l => l.UserId == userId),
                    ImageUrl = Url.Action("GetReceiptImageById", "Receipts", new { id = r.Id }, Request.Scheme)
                })
                .Take(50)
                .ToListAsync();

            return Ok(searchResults);
        }
    }
}