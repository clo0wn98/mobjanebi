using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;
using EShop.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        public async Task<IActionResult> Index()
        {
            var userId = await GetUserIdAsync();
            var viewModel = await GetCartViewModelAsync(userId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var userId = await GetUserIdAsync();
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ShoppingCartViewModel> GetCartViewModelAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p!.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var viewModel = new ShoppingCartViewModel();

            if (cart != null)
            {
                viewModel.ShoppingCart = cart;
                viewModel.CartItems = cart.CartItems.Where(i => i.Product != null).ToList();
                viewModel.TotalAmount = viewModel.CartItems.Sum(i => (i.Product!.DiscountPrice ?? i.Product.Price) * i.Quantity);
            }

            return viewModel;
        }
    }
}
