using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Data;
using System.Linq;

namespace EShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { success = false, message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "File size must be less than 5MB" });
            }

            var fileName = await SaveFileAsync(file, "products");
            var url = $"/uploads/products/{fileName}";

            return Ok(new { success = true, url = url });
        }

        [HttpPost("images")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> UploadImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { success = false, message = "No files uploaded" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var urls = new List<string>();

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(extension))
                {
                    continue;
                }
                
                if (file.Length > 5 * 1024 * 1024)
                {
                    continue;
                }

                var fileName = await SaveFileAsync(file, "products");
                urls.Add($"/uploads/products/{fileName}");
            }

            return Ok(new { success = true, urls = urls });
        }

        [HttpPost("video")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            var allowedExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { success = false, message = "Invalid file type. Allowed: mp4, webm, ogg, mov, avi" });
            }

            if (file.Length > 100 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "File size must be less than 100MB" });
            }

            var fileName = await SaveFileAsync(file, "videos");
            var url = $"/uploads/videos/{fileName}";

            return Ok(new { success = true, url = url });
        }

        [HttpPost("video-url")]
        public IActionResult SaveVideoUrl([FromBody] VideoUrlModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Url))
            {
                return BadRequest(new { success = false, message = "URL is required" });
            }

            return Ok(new { success = true, url = model.Url });
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            
            var uploadsFolder = Path.Combine(webRoot, "uploads", folder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLower()}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }
    }

    public class VideoUrlModel
    {
        public string Url { get; set; } = string.Empty;
    }
}
