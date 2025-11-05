using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recipe_Sharing_Platform_API.Data;
using Microsoft.AspNetCore.Authorization;

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

            var searchResults = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Where(r =>
                    r.Title.ToLower().Contains(query) ||
                    (r.User != null && r.User.Name.ToLower().Contains(query))
                )
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    UserName = r.User.Name,
                    LikesCount = r.Likes.Count,
                    ImageUrl = Url.Action("GetReceiptImageById", "Receipts", new { id = r.Id }, Request.Scheme)
                })
                .Take(50)
                .ToListAsync();

            return Ok(searchResults);
        }
    }
}