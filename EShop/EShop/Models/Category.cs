using System.ComponentModel.DataAnnotations;

namespace EShop.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Show in Navigation Menu")]
        public bool ShowInMenu { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
