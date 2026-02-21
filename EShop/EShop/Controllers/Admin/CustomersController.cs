using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;
using Microsoft.AspNetCore.Identity;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/Customers")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CustomersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string searchTerm, int pageNumber = 1)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            const int pageSize = 20;
            var totalUsers = await users.CountAsync();
            var usersPage = await users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in usersPage)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            ViewBag.CurrentSearch = searchTerm;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.UserRoles = userRoles;

            return View(usersPage);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders;

            return View(user);
        }
    }
}
