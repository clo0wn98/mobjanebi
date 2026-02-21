using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;
using EShop.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using EShop.Services;

namespace EShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ISmsService _smsService;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ISmsService smsService)
        {
            _context = context;
            _userManager = userManager;
            _smsService = smsService;
        }

        private async Task<string> GetUserIdAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return _userManager.GetUserId(User)!;
            }

            var sessionId = HttpContext.Session.GetString("AnonymousUserId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("AnonymousUserId", sessionId);
            }
            return sessionId;
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = await GetUserIdAsync();
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel
            {
                Cart = new ShoppingCartViewModel
                {
                    ShoppingCart = cart,
                    CartItems = cart.CartItems.Where(i => i.Product != null).ToList(),
                    TotalAmount = cart.CartItems.Where(i => i.Product != null).Sum(i => (i.Product!.DiscountPrice ?? i.Product.Price) * i.Quantity)
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order, string DiscountCode, decimal DiscountAmount)
        {
            var userId = await GetUserIdAsync();
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(order.FirstName) ||
                string.IsNullOrWhiteSpace(order.LastName) ||
                string.IsNullOrWhiteSpace(order.Phone) ||
                string.IsNullOrWhiteSpace(order.Address) ||
                string.IsNullOrWhiteSpace(order.City) ||
                string.IsNullOrWhiteSpace(order.PostalCode))
            {
                TempData["Error"] = "لطفاً تمام فیلدهای الزامی را تکمیل کنید";
                return RedirectToAction(nameof(Checkout));
            }

            order.UserId = userId;
            order.OrderDate = DateTime.Now;
            order.TotalAmount = cart.CartItems.Where(i => i.Product != null).Sum(i => (i.Product!.DiscountPrice ?? i.Product.Price) * i.Quantity);
            order.OrderStatus = "Pending";
            order.PaymentStatus = "Paid"; // COD - paid on delivery
            
            if (string.IsNullOrWhiteSpace(order.Email))
                order.Email = "info@mobjanebi.ir";
            if (string.IsNullOrWhiteSpace(order.Country))
                order.Country = "ایران";
            
            // Apply discount
            if (!string.IsNullOrEmpty(DiscountCode) && DiscountAmount > 0)
            {
                var discountCode = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == DiscountCode.Trim().ToUpper());
                if (discountCode != null)
                {
                    order.DiscountCodeId = discountCode.Id;
                    order.DiscountAmount = DiscountAmount;
                    discountCode.UsageCount++;
                }
            }
            
            order.FinalAmount = order.TotalAmount - order.DiscountAmount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cart.CartItems)
            {
                if (cartItem.Product != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.DiscountPrice ?? cartItem.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);

                    cartItem.Product.Stock -= cartItem.Quantity;
                }
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            // Send SMS to customer and admin
            await _smsService.SendOrderConfirmation(order.Phone, order.Id.ToString(), order.FinalAmount);
            await _smsService.SendToAdmin($"سفارش جدید #{order.Id} - مبلغ: {order.FinalAmount.ToString("N0")} تومان - مشتری: {order.Phone}");

            return RedirectToAction(nameof(OrderConfirmed), new { id = order.Id });
        }

        public async Task<IActionResult> OrderConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id.Value);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> MyOrders()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User)!;
            
            // Get user's phone number from Identity
            var user = await _userManager.FindByIdAsync(userId);
            var userPhone = user?.PhoneNumber;

            // Get orders by UserId OR by Phone number (if user placed order without login)
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId || (userPhone != null && o.Phone == userPhone))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.FindByIdAsync(userId);
            var userPhone = user?.PhoneNumber;

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id.Value && (o.UserId == userId || (userPhone != null && o.Phone == userPhone)));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
