using CLDV6212POE.Models;

namespace CLDV6212POE.ViewModels
{
    public class HomeViewModel
    {
        // List of products for the "Popular" slider
        public List<Product> PopularProducts { get; set; } = new();

        // List of products for the "New Arrivals" slider
        public List<Product> NewArrivals { get; set; } = new();
    }
}