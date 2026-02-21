using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;
using EShop.Models;

namespace EShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, string searchTerm, string sortBy = "name", int pageNumber = 1)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            sortBy = sortBy?.ToLower() ?? "name";
            products = sortBy switch
            {
                "price_low" => products.OrderBy(p => (double)p.Price),
                "price_high" => products.OrderByDescending(p => (double)p.Price),
                "newest" => products.OrderByDescending(p => p.CreatedAt),
                _ => products.OrderBy(p => p.Name)
            };

            const int pageSize = 12;
            var totalProducts = await products.CountAsync();
            var productsPage = await products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentSort = sortBy;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(productsPage);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Videos)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (product == null)
            {
                return NotFound();
            }

            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(product);
        }
    }
}
