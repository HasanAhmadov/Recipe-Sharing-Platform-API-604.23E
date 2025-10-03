using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace Recipe_Sharing_Platform_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        [HttpPost("upload")]
        [Consumes("multipart/form-data")] // 👈 important
        public async Task<IActionResult> Upload([FromForm] ReceiptUploadDto dto)
        {
            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("No file uploaded.");

            using var ms = new MemoryStream();
            await dto.Image.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            // Example: save into DB as Receipt
            var receipt = new Receipt
            {
                Title = dto.Title,
                Image = fileBytes
            };

            // save to db...
            // _context.Receipts.Add(receipt);
            // await _context.SaveChangesAsync();

            return Ok(new { message = "Uploaded successfully", receipt.Title });
        }
    }

    public class ReceiptUploadDto
    {
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// File to upload
        /// </summary>
        public IFormFile Image { get; set; } = default!;
    }

}