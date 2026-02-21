using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EShop.Data;
using EShop.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EShop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/DiscountCodes")]
    public class DiscountCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var codes = await _context.DiscountCodes
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return View(codes);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscountCode model)
        {
            if (ModelState.IsValid)
            {
                model.Code = model.Code.Trim().ToUpper();
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "کد تخفیف با موفقیت ایجاد شد";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("Edit/{id?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var code = await _context.DiscountCodes.FindAsync(id);
            if (code == null) return NotFound();
            return View(code);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DiscountCode model)
        {
            if (id != model.Id) return NotFound();
            
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.DiscountCodes.FindAsync(id);
                    existing.Code = model.Code.Trim().ToUpper();
                    existing.Description = model.Description;
                    existing.DiscountType = model.DiscountType;
                    existing.DiscountValue = model.DiscountValue;
                    existing.MinOrderAmount = model.MinOrderAmount;
                    existing.MaxDiscountAmount = model.MaxDiscountAmount;
                    existing.EndDate = model.EndDate;
                    existing.MaxUsageCount = model.MaxUsageCount;
                    existing.IsActive = model.IsActive;
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.DiscountCodes.AnyAsync(e => e.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("Delete/{id?}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var code = await _context.DiscountCodes.FindAsync(id);
            if (code == null) return NotFound();
            return View(code);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var code = await _context.DiscountCodes.FindAsync(id);
            if (code != null)
            {
                _context.DiscountCodes.Remove(code);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ValidateCode")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateCode(string code, decimal orderAmount)
        {
            var discountCode = await _context.DiscountCodes
                .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpper() && c.IsActive);

            if (discountCode == null)
            {
                return Json(new { valid = false, message = "کد تخفیف نامعتبر است" });
            }

            if (discountCode.EndDate < DateTime.Now)
            {
                return Json(new { valid = false, message = "تاریخ انقضای کد تخفیف گذشته است" });
            }

            if (discountCode.MaxUsageCount.HasValue && discountCode.UsageCount >= discountCode.MaxUsageCount.Value)
            {
                return Json(new { valid = false, message = "ظرفیت استفاده از کد تخفیف تکمیل شده است" });
            }

            if (discountCode.MinOrderAmount.HasValue && orderAmount < discountCode.MinOrderAmount.Value)
            {
                return Json(new { valid = false, message = $"حداقل مبلغ خرید باید {discountCode.MinOrderAmount.Value:N0} تومان باشد" });
            }

            decimal discount = discountCode.DiscountType == "Percent" 
                ? orderAmount * (discountCode.DiscountValue / 100)
                : discountCode.DiscountValue;

            if (discountCode.MaxDiscountAmount.HasValue && discount > discountCode.MaxDiscountAmount.Value)
            {
                discount = discountCode.MaxDiscountAmount.Value;
            }

            return Json(new { 
                valid = true, 
                discount = discount,
                message = "کد تخفیف با موفقیت اعمال شد"
            });
        }
    }
}
