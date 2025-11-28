using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recipe_Sharing_Platform_API.DTO;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.Models;
using Microsoft.AspNetCore.Authorization;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReceiptsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("GetAllReceipts")]
        public async Task<IActionResult> GetAllReceipts()
        {
            var receipts = await _context.Recipes
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    r.User.Username,
                    r.Likes.Count,
                    likedByUser = r.Likes.Any(l => l.UserId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)),
                    ImageUrl = $"/api/Receipts/GetReceiptImageById/{r.Id}"
                })
                .ToListAsync();

            return Ok(receipts);
        }

        [HttpGet("GetReceiptsByUserId/{userId}")]
        public async Task<IActionResult> GetReceiptsByUserId(int userId)
        {
            var userReceipts = await _context.Recipes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    r.User.Username,
                    r.Likes.Count,
                    likedByUser = r.Likes.Any(l => l.UserId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)),
                    ImageUrl = $"/api/Receipts/GetReceiptImageById/{r.Id}"
                })
                .ToListAsync();

            return Ok(userReceipts);
        }

        [Consumes("multipart/form-data")]
        [HttpPost("UploadReceipt")]
        public async Task<IActionResult> Upload([FromForm] ReceiptUpload dto)
        {
            if (dto.Image == null || dto.Image.Length == 0) return BadRequest("No file uploaded.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("Invalid or missing token.");

            int userId = int.Parse(userIdClaim.Value);

            using var ms = new MemoryStream();
            await dto.Image.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            var receipt = new Receipt
            {
                Title = dto.Title,
                Image = fileBytes,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Recipes.Add(receipt); // or Recipes in Database
            await _context.SaveChangesAsync();

            return Ok(new { message = "Uploaded successfully", receipt.Title, UserId = userId });
        }

        [AllowAnonymous]
        [HttpGet("GetReceiptById/{id}")]
        public async Task<IActionResult> GetReceiptById(int id)
        {
            var receipt = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Likes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();

            return Ok(new
            {
                receipt.Id,
                receipt.Title,
                receipt.CreatedAt,
                receipt.UserId,
                UserName = receipt.User?.Name,
                LikesCount = receipt.Likes.Count,
                ImageUrl = Url.Action(nameof(GetReceiptImageById), new { id = receipt.Id }) // link to actual image
            });
        }

        [AllowAnonymous]
        [HttpGet("GetReceiptImageById/{id}")]
        public async Task<IActionResult> GetReceiptImageById(int id)
        {
            var receipt = await _context.Recipes.FindAsync(id);
            if (receipt == null || receipt.Image == null)
                return NotFound();

            // Return raw image bytes with MIME type
            return File(receipt.Image, "image/jpeg"); // or image/png if needed
        }

        [HttpDelete("DeleteReceiptById/{id}")]
        public async Task<IActionResult> DeleteReceiptById(int id)
        {
            var receipt = await _context.Recipes
                .Include(r => r.Likes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
                return NotFound($"Receipt with ID {id} not found.");

            if (receipt.Likes.Count != 0) _context.Likes.RemoveRange(receipt.Likes);
            
            _context.Recipes.Remove(receipt);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Receipt with ID {id} deleted successfully." });
        }

        [HttpDelete("DeleteReceiptsByIds")]
        public async Task<IActionResult> DeleteReceiptsByIds([FromBody] int[] receiptIds)
        {
            if (receiptIds == null || receiptIds.Length == 0)
                return BadRequest("No receipt IDs provided.");

            var receiptsToDelete = await _context.Recipes
                .Include(r => r.Likes)
                .Where(r => receiptIds.Contains(r.Id))
                .ToListAsync();

            if (receiptsToDelete.Count == 0)
                return NotFound("No receipts found with the provided IDs.");

            var allLikes = receiptsToDelete.SelectMany(r => r.Likes).ToList();
            if (allLikes.Count != 0) _context.Likes.RemoveRange(allLikes);
            
            _context.Recipes.RemoveRange(receiptsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"{receiptsToDelete.Count} receipts deleted successfully.",
                deletedReceipts = receiptsToDelete.Select(r => r.Id).ToList()
            });
        }
    }
}