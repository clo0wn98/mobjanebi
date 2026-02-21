using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;

namespace EShop.ViewComponents
{
    public class MenuCategoriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public MenuCategoriesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.ShowInMenu)
                .ToListAsync();
            return View(categories);
        }
    }
}
