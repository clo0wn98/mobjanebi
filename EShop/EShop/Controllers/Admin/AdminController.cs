using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            var orders = await _context.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .ToListAsync();
            ViewBag.TotalRevenue = orders.Sum(o => o.FinalAmount > 0 ? o.FinalAmount : o.TotalAmount);
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            ViewBag.TotalCustomers = customers.Count;

            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}
