using Microsoft.AspNetCore.Mvc;
using EShop.Data;
using Microsoft.EntityFrameworkCore;

namespace EShop.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var sessionId = HttpContext.Session.GetString("AnonymousUserId");
            var userId = User.Identity?.IsAuthenticated == true 
                ? User.Identity.Name 
                : sessionId;

            int count = 0;
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await _context.ShoppingCarts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                count = cart?.CartItems.Sum(i => i.Quantity) ?? 0;
            }

            return View(count);
        }
    }
}
