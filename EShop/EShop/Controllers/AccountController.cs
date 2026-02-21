using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EShop.Models.Account;
using EShop.Data;
using EShop.Services;

namespace EShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;

        public AccountController(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            ISmsService smsService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _smsService = smsService;
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
            }

            ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است");
            return View(model);
        }

        // Phone Login - Send Code
        [HttpGet]
        public IActionResult LoginWithPhone(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationCode(SendCodeViewModel model)
        {
            if (string.IsNullOrEmpty(model.PhoneNumber))
            {
                return Json(new { success = false, message = "شماره تلفن را وارد کنید" });
            }

            // Generate random 6-digit code
            var code = new Random().Next(100000, 999999).ToString();

            // Save verification code to database
            var verification = new Models.PhoneVerification
            {
                PhoneNumber = model.PhoneNumber,
                VerificationCode = code,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(5)
            };

            _context.PhoneVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // Send SMS
            var smsSent = await _smsService.SendVerificationCode(model.PhoneNumber, code);

            if (smsSent)
            {
                return Json(new { success = true, message = "کد تأیید ارسال شد", debugCode = code });
            }
            else
            {
                // SMS failed, still return code for testing
                return Json(new { success = true, message = "کد تأیید (پیامک خطا داشت)", debugCode = code, smsFailed = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAndLogin(PhoneLoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(model.PhoneNumber) || string.IsNullOrEmpty(model.VerificationCode))
            {
                ModelState.AddModelError(string.Empty, "شماره تلفن و کد تأیید را وارد کنید");
                return View("LoginWithPhone", model);
            }

            var verification = await _context.PhoneVerifications
                .Where(v => v.PhoneNumber == model.PhoneNumber 
                    && v.VerificationCode == model.VerificationCode 
                    && !v.IsUsed 
                    && v.ExpiresAt > DateTime.Now)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
            {
                ModelState.AddModelError(string.Empty, "کد تأیید نامعتبر یا منقضی شده است");
                return View("LoginWithPhone", model);
            }

            // Mark code as used
            verification.IsUsed = true;
            await _context.SaveChangesAsync();

            // Find or create user
            var user = await _userManager.FindByNameAsync(model.PhoneNumber);
            if (user == null)
            {
                // Create new user with phone as username
                user = new IdentityUser
                {
                    UserName = model.PhoneNumber,
                    PhoneNumber = model.PhoneNumber,
                    PhoneNumberConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, "Phone" + model.PhoneNumber.Substring(model.PhoneNumber.Length - 4));
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "خطا در ایجاد حساب کاربری");
                    return View("LoginWithPhone", model);
                }
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToLocal(returnUrl);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
