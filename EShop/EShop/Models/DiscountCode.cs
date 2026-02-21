using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Models
{
    public class DiscountCode
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "کد تخفیف")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "نوع تخفیف")]
        public string DiscountType { get; set; } = "Percent"; // Percent, Fixed

        [Display(Name = "مقدار تخفیف")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "حداقل مبلغ خرید")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "حداکثر مبلغ تخفیف")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Display(Name = "تاریخ شروع")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "تاریخ پایان")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

        [Display(Name = "حداکثر تعداد استفاده")]
        public int? MaxUsageCount { get; set; }

        [Display(Name = "تعداد استفاده شده")]
        public int UsageCount { get; set; } = 0;

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "مخصوص کاربران جدید")]
        public bool IsForNewUsers { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
