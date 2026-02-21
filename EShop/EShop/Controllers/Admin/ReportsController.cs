using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/Reports")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var paidOrders = await _context.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .ToListAsync();
            var totalRevenue = paidOrders.Sum(o => o.FinalAmount > 0 ? o.FinalAmount : o.TotalAmount);
            var totalProducts = await _context.Products.CountAsync();
            var lowStockProducts = await _context.Products.Where(p => p.Stock < 10).CountAsync();

            var monthlyOrders = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-6))
                .ToListAsync();

            var monthlySales = monthlyOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Total = g.Sum(o => o.FinalAmount > 0 ? o.FinalAmount : o.TotalAmount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(6)
                .ToList();

            var topProducts = await _context.OrderItems
                .Include(o => o.Product)
                .GroupBy(o => o.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product!.Name,
                    TotalSold = g.Sum(o => o.Quantity),
                    TotalRevenue = g.Sum(o => o.Price * o.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.MonthlySales = monthlySales;
            ViewBag.TopProducts = topProducts;

            return View();
        }

        [HttpGet("Profit")]
        public async Task<IActionResult> Profit(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.PaymentStatus == "Paid")
                .ToListAsync();

            decimal totalRevenue = 0;
            decimal estimatedProfit = 0;

            foreach (var order in orders)
            {
                totalRevenue += order.FinalAmount > 0 ? order.FinalAmount : order.TotalAmount;
                foreach (var item in order.OrderItems)
                {
                    var estimatedCost = item.Price * 0.6m;
                    estimatedProfit += (item.Price - estimatedCost) * item.Quantity;
                }
            }

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.EstimatedProfit = estimatedProfit;
            ViewBag.OrdersCount = orders.Count;

            return View();
        }
    }
}
