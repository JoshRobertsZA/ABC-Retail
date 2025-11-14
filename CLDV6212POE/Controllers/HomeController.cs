using CLDV6212POE.Data;
using CLDV6212POE.Models;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using CLDV6212POE.ViewModels;

namespace CLDV6212POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly TableStorageService<Product> _productService;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, TableStorageService<Product> productService)
        {
            _logger = logger;
            _context = context;
            _productService = productService;

        }
        public async Task<IActionResult> Index(string? search = null)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Redirect to Product page with search query
                return RedirectToAction("Index", "Product", new { search = search });
            }

            // Get all products from the database
            var allProducts = await _productService.GetAllEntitiesAsync();

            // Pick the first 6 as popular products
            var popularProducts = allProducts.Take(6).ToList();

            // Pick the next 6 as new arrivals
            var newArrivals = allProducts.Skip(6).Take(6).ToList();

            var viewModel = new HomeViewModel
            {
                PopularProducts = popularProducts,
                NewArrivals = newArrivals
            };

            return View(viewModel);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
