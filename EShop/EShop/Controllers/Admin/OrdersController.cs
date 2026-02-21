using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Data;
using EShop.Services;
using Microsoft.EntityFrameworkCore;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/Orders")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;

        public OrdersController(ApplicationDbContext context, ISmsService smsService)
        {
            _context = context;
            _smsService = smsService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        [HttpGet("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string orderStatus, string paymentStatus, string? trackingCode)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.OrderStatus = orderStatus;
                order.PaymentStatus = paymentStatus;
                
                // If tracking code is provided, update it and send SMS
                if (!string.IsNullOrWhiteSpace(trackingCode))
                {
                    order.TrackingCode = trackingCode;
                    order.ShippedDate = DateTime.Now;
                    
                    // Send SMS to customer with tracking code
                    await _smsService.SendTrackingCode(order.Phone, order.Id.ToString(), trackingCode);
                }
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
