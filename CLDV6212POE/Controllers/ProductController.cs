using CLDV6212POE.Services;
using Function.Models;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212POE.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService<Product> _productService;
        private readonly FunctionConnector _functionConnector;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            TableStorageService<Product> productService,
            FunctionConnector functionConnector,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _functionConnector = functionConnector;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllEntitiesAsync();
            return View(products ?? new List<Product>());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View(new Product());
        }

        [HttpPost]
        public async Task<IActionResult> Add(
            string productName,
            string description,
            string price,
            string category,
            int stockQuantity,
            IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(productName) ||
                string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(price) ||
                string.IsNullOrWhiteSpace(category))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View(new Product());
            }

            string? imageUrl = null;
            if (image is { Length: > 0 })
            {
                _logger.LogInformation("Uploading image: {FileName} ({Size} bytes)", image.FileName, image.Length);

                imageUrl = await _functionConnector.UploadToBlobAsync(image);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogError("Image upload failed via Azure Function");
                    ModelState.AddModelError(string.Empty, "Image upload failed. Please try again.");
                    return View(new Product());
                }

                _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
            }

            var newProduct = new Product
            {
                PartitionKey = "Product",
                RowKey = Guid.NewGuid().ToString(),
                ProductName = productName,
                Description = description,
                Price = price,
                Category = category,
                StockQuantity = stockQuantity,
                ImageUrl = imageUrl,
                CreatedDate = DateTime.UtcNow
            };

            var success = await _functionConnector.StoreProductAsync(newProduct);

            if (!success)
            {
                _logger.LogError("Failed to store product via Azure Function");
                ModelState.AddModelError(string.Empty, "Could not save product. Please try again.");
                return View(newProduct);
            }

            _logger.LogInformation("Product created successfully: {ProductName}", productName);
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            var product = await _productService.GetEntityAsync("Product", rowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product updatedProduct, IFormFile? image)
        {
            var existing = await _productService.GetEntityAsync("Product", updatedProduct.RowKey);
            if (existing == null)
                return NotFound();

            existing.ProductName = updatedProduct.ProductName;
            existing.Description = updatedProduct.Description;
            existing.Price = updatedProduct.Price;
            existing.Category = updatedProduct.Category;
            existing.StockQuantity = updatedProduct.StockQuantity;

            if (image != null && image.Length > 0)
            {
                // Validate image file
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    ModelState.AddModelError(nameof(image), "Only JPEG, PNG, and GIF images are allowed.");
                    return View(existing);
                }

                // Upload via Azure Function
                var newImageUrl = await _functionConnector.UploadToBlobAsync(image);
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    existing.ImageUrl = newImageUrl;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to upload new image via Azure Function.");
                    return View(existing);
                }
            }

            await _productService.UpdateEntityAsync(existing);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            var existing = await _productService.GetEntityAsync("Product", rowKey);
            if (existing == null)
                return NotFound();

            await _productService.DeleteEntityAsync("Product", rowKey);
            return RedirectToAction("Index");
        }
    }
}