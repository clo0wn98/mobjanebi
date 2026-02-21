using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EShop.Data;
using EShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Products")]
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? categoryId, string searchTerm)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = searchTerm;

            var productList = await products.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(productList);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<string> attributeNames, List<string> attributeValues, 
            IFormFile? MainImage, List<IFormFile>? AdditionalImages, IFormFile? ProductVideo, string VideoUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(product.Name) || product.Price <= 0 || product.CategoryId <= 0)
                {
                    TempData["Error"] = "لطفاً نام محصول، قیمت و دسته‌بندی را وارد کنید";
                    ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                    return View(product);
                }

                product.CreatedAt = DateTime.Now;
                product.Stock = product.Stock > 0 ? product.Stock : 0;
                
                // Handle main image upload
                if (MainImage != null && MainImage.Length > 0)
                {
                    var imageUrl = await SaveUploadedFile(MainImage, "products");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        product.ImageUrl = imageUrl;
                    }
                }
                // Handle main image from URL
                else if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.StartsWith("http"))
                {
                    // URL is already set, keep it
                }
                
                _context.Add(product);
                await _context.SaveChangesAsync();

                // Add additional images from file upload (NOT the main image - that's in Products table)
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    int order = 1;
                    foreach (var img in AdditionalImages)
                    {
                        var imgUrl = await SaveUploadedFile(img, "products");
                        if (!string.IsNullOrEmpty(imgUrl))
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = imgUrl,
                                IsMain = false,
                                Order = order++
                            });
                        }
                    }
                }

                // Add video from file upload
                if (ProductVideo != null && ProductVideo.Length > 0)
                {
                    var videoUrl = await SaveUploadedFile(ProductVideo, "videos");
                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        _context.ProductVideos.Add(new ProductVideo
                        {
                            ProductId = product.Id,
                            VideoUrl = videoUrl
                        });
                    }
                }

                // Add video from URL
                if (!string.IsNullOrWhiteSpace(VideoUrl))
                {
                    _context.ProductVideos.Add(new ProductVideo
                    {
                        ProductId = product.Id,
                        VideoUrl = VideoUrl
                    });
                }

                // Add attributes if provided
                if (attributeNames != null && attributeValues != null)
                {
                    for (int i = 0; i < attributeNames.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(attributeNames[i]) && !string.IsNullOrEmpty(attributeValues[i]))
                        {
                            _context.ProductAttributes.Add(new ProductAttribute
                            {
                                ProductId = product.Id,
                                Name = attributeNames[i],
                                Value = attributeValues[i]
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "محصول با موفقیت افزوده شد";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در ثبت محصول: " + ex.Message;
                ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }
        }

        [HttpGet("Edit/{id?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Attributes)
                .Include(p => p.Images)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            
            if (product == null) return NotFound();

            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<string> attributeNames, List<string> attributeValues, 
            IFormFile? MainImage, List<IFormFile>? AdditionalImages, IFormFile? ProductVideo, string VideoUrl)
        {
            if (id != product.Id) return NotFound();

            string debugLog = $"Edit POST called. MainImage: {MainImage?.FileName} ({MainImage?.Length} bytes), Additional: {AdditionalImages?.Count ?? 0}";

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null) return NotFound();

                // Update basic fields
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.DiscountPrice = product.DiscountPrice;
                existingProduct.Stock = product.Stock;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.SKU = product.SKU;

                // Handle main image upload - NEW FILE SELECTED
                if (MainImage != null && MainImage.Length > 0)
                {
                    debugLog += $" | Processing main image: {MainImage.FileName}";
                    
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl.StartsWith("/uploads/"))
                    {
                        var oldPath = Path.Combine(_environment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                            debugLog += " | Deleted old image";
                        }
                    }

                    // Save new file
                    var extension = Path.GetExtension(MainImage.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await MainImage.CopyToAsync(stream);
                    }

                    var newImageUrl = $"/uploads/products/{fileName}";
                    existingProduct.ImageUrl = newImageUrl;
                    debugLog += $" | Saved to: {newImageUrl}";
                }
                else
                {
                    debugLog += " | No new main image uploaded";
                }

                // Handle additional images
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    debugLog += $" | Processing {AdditionalImages.Count} additional images";
                    foreach (var img in AdditionalImages)
                    {
                        if (img.Length > 0)
                        {
                            var extension = Path.GetExtension(img.FileName).ToLower();
                            var fileName = $"{Guid.NewGuid()}{extension}";
                            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                            
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            var filePath = Path.Combine(uploadsFolder, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await img.CopyToAsync(stream);
                            }

                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = id,
                                ImageUrl = $"/uploads/products/{fileName}",
                                IsMain = false
                            });
                        }
                    }
                }

                // Handle video
                if (ProductVideo != null && ProductVideo.Length > 0)
                {
                    var extension = Path.GetExtension(ProductVideo.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "videos");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProductVideo.CopyToAsync(stream);
                    }

                    _context.ProductVideos.Add(new ProductVideo
                    {
                        ProductId = id,
                        VideoUrl = $"/uploads/videos/{fileName}"
                    });
                }

                // Handle video URL
                if (!string.IsNullOrWhiteSpace(VideoUrl))
                {
                    _context.ProductVideos.Add(new ProductVideo
                    {
                        ProductId = id,
                        VideoUrl = VideoUrl
                    });
                }

                // Update attributes
                var existingAttributes = await _context.ProductAttributes.Where(a => a.ProductId == id).ToListAsync();
                _context.ProductAttributes.RemoveRange(existingAttributes);

                if (attributeNames != null && attributeValues != null)
                {
                    for (int i = 0; i < attributeNames.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(attributeNames[i]) && !string.IsNullOrEmpty(attributeValues[i]))
                        {
                            _context.ProductAttributes.Add(new ProductAttribute
                            {
                                ProductId = id,
                                Name = attributeNames[i],
                                Value = attributeValues[i]
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                debugLog += " | SAVED SUCCESSFULLY!";
                
                TempData["Debug"] = debugLog;
                TempData["Success"] = "محصول با موفقیت ویرایش شد";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                debugLog += $" | ERROR: {ex.Message}";
                TempData["Debug"] = debugLog;
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpGet("Delete/{id?}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Remove attributes first
                var attributes = await _context.ProductAttributes.Where(a => a.ProductId == id).ToListAsync();
                _context.ProductAttributes.RemoveRange(attributes);
                
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "محصول با موفقیت حذف شد";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("DeleteMainImage/{id?}")]
        public async Task<IActionResult> DeleteMainImage(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id.Value);
            if (product == null) return NotFound();

            // Delete file from disk
            if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            product.ImageUrl = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تصویر اصلی با موفقیت حذف شد";
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpGet("DeleteImage/{id?}")]
        public async Task<IActionResult> DeleteImage(int? id)
        {
            if (id == null) return NotFound();

            var image = await _context.ProductImages.FindAsync(id.Value);
            if (image == null) return NotFound();

            var productId = image.ProductId;

            // Delete file from disk
            if (!string.IsNullOrEmpty(image.ImageUrl) && image.ImageUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تصویر با موفقیت حذف شد";
            return RedirectToAction("Edit", new { id = productId });
        }

        [HttpGet("DeleteVideo/{id?}")]
        public async Task<IActionResult> DeleteVideo(int? id)
        {
            if (id == null) return NotFound();

            var video = await _context.ProductVideos.FindAsync(id.Value);
            if (video == null) return NotFound();

            var productId = video.ProductId;

            // Delete file from disk
            if (!string.IsNullOrEmpty(video.VideoUrl) && video.VideoUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_environment.WebRootPath, video.VideoUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.ProductVideos.Remove(video);
            await _context.SaveChangesAsync();

            TempData["Success"] = "ویدئو با موفقیت حذف شد";
            return RedirectToAction("Edit", new { id = productId });
        }

        [HttpGet("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product == null) return NotFound();

            return View(product);
        }

        private async Task<string> SaveUploadedFile(IFormFile file, string folder)
        {
            ViewBag.Debug = (ViewBag.Debug ?? "") + $"SaveUploadedFile called: {file?.FileName}, {file?.Length} bytes. ";
            
            if (file == null || file.Length == 0) 
            {
                ViewBag.Debug += "File is null or empty. ";
                return string.Empty;
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            
            var uploadsFolder = Path.Combine(webRoot, "uploads", folder);
            ViewBag.Debug += $"uploadsFolder: {uploadsFolder}. ";
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            ViewBag.Debug += $"extension: {extension}. ";
            
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedVideoExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi" };
            
            if (folder == "products" && !allowedImageExtensions.Contains(extension))
            {
                ViewBag.Debug += $"Extension {extension} not allowed for products! ";
                return string.Empty;
            }
            if (folder == "videos" && !allowedVideoExtensions.Contains(extension))
            {
                ViewBag.Debug += $"Extension {extension} not allowed for videos! ";
                return string.Empty;
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            ViewBag.Debug += $"filePath: {filePath}. ";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = $"/uploads/{folder}/{fileName}";
            ViewBag.Debug += $"Returning: {result}. ";
            return result;
        }
    }
}
