using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Data;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/Settings")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var adminPhones = await _context.AdminSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "AdminPhones");
            
            if (adminPhones == null)
            {
                adminPhones = new Models.AdminSettings
                {
                    SettingKey = "AdminPhones",
                    SettingValue = ""
                };
                _context.AdminSettings.Add(adminPhones);
                await _context.SaveChangesAsync();
            }

            return View(adminPhones);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string adminPhones)
        {
            var setting = await _context.AdminSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "AdminPhones");
            
            if (setting != null)
            {
                setting.SettingValue = adminPhones;
                setting.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.AdminSettings.Add(new Models.AdminSettings
                {
                    SettingKey = "AdminPhones",
                    SettingValue = adminPhones,
                    UpdatedAt = DateTime.Now
                });
            }
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "شماره‌های مدیران با موفقیت ذخیره شد";
            return RedirectToAction(nameof(Index));
        }
    }
}
