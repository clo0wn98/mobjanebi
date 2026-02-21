namespace EShop.Models
{
    public class ShoppingCartViewModel
    {
        public ShoppingCart ShoppingCart { get; set; } = new();
        public List<CartItem> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class CheckoutViewModel
    {
        public ShoppingCartViewModel Cart { get; set; } = new();
        public Order Order { get; set; } = new();
    }

    public class ProductViewModel
    {
        public Product Product { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }

    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> LatestProducts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
