using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EShop.Models;

namespace EShop.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            if (context.Categories.Any())
            {
                return;
            }

            var categories = new List<Category>
            {
                new Category { Name = "الکترونیک", Description = "دستگاه‌ها و گجت‌های الکترونیکی", ImageUrl = "https://picsum.photos/seed/electronics/400/300" },
                new Category { Name = "پوشاک", Description = "مد و لباس", ImageUrl = "https://picsum.photos/seed/clothing/400/300" },
                new Category { Name = "خانه و باغ", Description = "لوازم دکوری و باغبانی", ImageUrl = "https://picsum.photos/seed/home/400/300" },
                new Category { Name = "ورزشی", Description = "تجهیزات و لوازم ورزشی", ImageUrl = "https://picsum.photos/seed/sports/400/300" },
                new Category { Name = "کتاب", Description = "کتاب و نشریات", ImageUrl = "https://picsum.photos/seed/books/400/300" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Name = "هدفون بی‌سیم", Description = "هدفون بی‌سیم با حذف نویز", Price = 5000000m, DiscountPrice = 3800000m, Stock = 50, CategoryId = 1, IsFeatured = true, ImageUrl = "https://picsum.photos/seed/headphones/400/400" },
                new Product { Name = "ساعت هوشمند", Description = "ساعت هوشمند با قابلیت‌های سلامتی", Price = 8500000m, Stock = 30, CategoryId = 1, IsFeatured = true, ImageUrl = "https://picsum.photos/seed/smartwatch/400/400" },
                new Product { Name = "پایه لپ‌تاپ", Description = "پایه آلومینیومی ارگونومیک", Price = 1200000m, Stock = 100, CategoryId = 1, ImageUrl = "https://picsum.photos/seed/laptopstand/400/400" },
                new Product { Name = "پیراهن مردانه", Description = "پیراهن نخی راحت", Price = 950000m, DiscountPrice = 720000m, Stock = 75, CategoryId = 2, IsFeatured = true, ImageUrl = "https://picsum.photos/seed/shirt/400/400" },
                new Product { Name = "پیراهن زنانه", Description = "پیراهن تابستانی شیک", Price = 1850000m, Stock = 40, CategoryId = 2, ImageUrl = "https://picsum.photos/seed/dress/400/400" },
                new Product { Name = "کفش ورزشی", Description = "کفش دو سبک و راحت", Price = 3200000m, Stock = 60, CategoryId = 4, IsFeatured = true, ImageUrl = "https://picsum.photos/seed/shoes/400/400" },
                new Product { Name = "مت یوگا", Description = "مت یوگا ضد لغزش", Price = 680000m, Stock = 80, CategoryId = 4, ImageUrl = "https://picsum.photos/seed/yoga/400/400" },
                new Product { Name = "میز قهوه", Description = "میز چوبی مدرن", Price = 4500000m, Stock = 20, CategoryId = 3, ImageUrl = "https://picsum.photos/seed/table/400/400" },
                new Product { Name = "چراغ مطالعه", Description = "چراغ LED با نور قابل تنظیم", Price = 980000m, Stock = 55, CategoryId = 3, ImageUrl = "https://picsum.photos/seed/lamp/400/400" },
                new Product { Name = "کتاب برنامه‌نویسی", Description = "راهنمای کامل برنامه‌نویسی مدرن", Price = 1250000m, Stock = 45, CategoryId = 5, IsFeatured = true, ImageUrl = "https://picsum.photos/seed/book/400/400" },
                new Product { Name = "اسپیکر بلوتوث", Description = "اسپیکر بلوتوث ضد آب", Price = 1850000m, DiscountPrice = 1400000m, Stock = 70, CategoryId = 1, ImageUrl = "https://picsum.photos/seed/speaker/400/400" },
                new Product { Name = "کوله‌پشتی", Description = "کوله‌پشتی مسافرتی با محفظه لپ‌تاپ", Price = 2100000m, Stock = 35, CategoryId = 2, ImageUrl = "https://picsum.photos/seed/backpack/400/400" }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}
