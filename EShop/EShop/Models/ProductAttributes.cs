using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "نام ویژگی")]
        public string Name { get; set; } = string.Empty; // رنگ، سایز، etc.

        [Display(Name = "مقدار")]
        public string Value { get; set; } = string.Empty; // قرمز، XL، etc.

        [Display(Name = "شناسه محصول")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }

    public class ProductImage
    {
        public int Id { get; set; }

        [Display(Name = "آدرس تصویر")]
        public string ImageUrl { get; set; } = string.Empty;

        [Display(Name = "ترتیب")]
        public int Order { get; set; } = 0;

        [Display(Name = "تصویر اصلی")]
        public bool IsMain { get; set; } = false;

        [Display(Name = "شناسه محصول")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }

    public class ProductVideo
    {
        public int Id { get; set; }

        [Display(Name = "آدرس ویدئو")]
        public string VideoUrl { get; set; } = string.Empty;

        [Display(Name = "عنوان")]
        public string? Title { get; set; }

        [Display(Name = "شناسه محصول")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
