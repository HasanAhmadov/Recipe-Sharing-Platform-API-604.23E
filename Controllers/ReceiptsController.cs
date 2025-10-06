﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    r.User.Username,
                    r.Likes.Count,
                    ImageUrl = $"/api/Receipts/GetReceiptImage/{r.Id}"
                })
                .ToListAsync();

            return Ok(receipts);
        }

        [HttpGet("GetReceiptsByUserId/{userId}")]
        public async Task<IActionResult> GetReceiptsByUserId(int userId)
        {
            var userReceipts = await _context.Recipes
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.CreatedAt,
                    r.UserId,
                    r.User.Username,
                    r.Likes.Count,
                    ImageUrl = $"/api/Receipts/GetReceiptImage/{r.Id}"
                })
                .ToListAsync();

            return Ok(userReceipts);
        }

        [Consumes("multipart/form-data")]
        [HttpPost("UploadReceipt")]
        public async Task<IActionResult> Upload([FromForm] ReceiptUpload dto)
        {
            if (dto.Image == null || dto.Image.Length == 0) return BadRequest("No file uploaded.");

            // ✅ Get current user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("Invalid or missing token.");

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
            return File(receipt.Image, "image/jpeg"); // or "image/png" if needed
        }
    }
}