using Azure.Data.Tables;
using CLDV6212POE.Models;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212POE.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService<Product> _productService;
        private readonly FunctionConnector _functionConnector;
        private readonly ILogger<ProductController> _logger;
        private readonly IConfiguration _config;

        public ProductController(
            IConfiguration config,
            TableStorageService<Product> productService,
            FunctionConnector functionConnector,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _functionConnector = functionConnector;
            _logger = logger;
            _config = config;
        }

        // Displays all products and applies optional search filtering.
        public async Task<IActionResult> Index(string? search = null)
        {
            var products = await _productService.GetAllEntitiesAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products
                    .Where(p =>
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.Category) && p.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
                    )
                    .ToList();
            }

            ViewData["SearchQuery"] = search;

            return View(products);
        }


        // Performs AJAX-based search and returns a filtered product list partial.
        public async Task<IActionResult> Search(string q)
        {
            var products = await _productService.GetAllEntitiesAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.ToLower();

                products = products
                    .Where(p =>
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(q)) ||
                        (!string.IsNullOrEmpty(p.Category) && p.Category.ToLower().Contains(q))
                    )
                    .ToList();
            }

            return PartialView("_ProductListPartial", products);
        }


        // Converts price strings to numeric values for sorting.
        private decimal ParsePrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return 0;

            // Remove anything except digits and decimal point
            var clean = new string(price.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return decimal.TryParse(clean, out var val) ? val : 0;
        }



        // Sorts products by price (high-to-low or low-to-high) and returns an updated partial view.
        public async Task<IActionResult> Sort(string sortType, string q)
        {
            var products = await _productService.GetAllEntitiesAsync();

            // Apply search if text present
            if (!string.IsNullOrEmpty(q))
            {
                products = products
                    .Where(p => p.ProductName.Contains(q, StringComparison.OrdinalIgnoreCase)
                                || p.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply numeric sorting
            switch (sortType)
            {
                case "hightolow":
                    products = products
                        .OrderByDescending(p => ParsePrice(p.Price))
                        .ToList();
                    break;

                case "lowtohigh":
                    products = products
                        .OrderBy(p => ParsePrice(p.Price))
                        .ToList();
                    break;
            }

            return PartialView("_ProductListPartial", products);
        }


        // Handles product creation, including optional blob image upload.
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


        // Retrieves a single product and returns the product details modal partial.
        [HttpGet]
        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return BadRequest("RowKey is required.");

            try
            {
                string connectionString = _config.GetConnectionString("AzureStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, "Storage configuration is missing.");
                }

                var tableClient = new TableClient(connectionString, "Product");

                Product? product = null;
                await foreach (var entity in tableClient.QueryAsync<Product>(p => rowKey != null && p.RowKey == rowKey))
                {
                    product = entity;
                    break;
                }

                if (product == null)
                    return NotFound("Product not found.");

                return PartialView("_ProductDetail", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details for RowKey: {RowKey}", rowKey);
                return StatusCode(500, "An error occurred while fetching the product details.");
            }
        }


        // Returns the edit page for a selected product.
        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            var product = await _productService.GetEntityAsync("Product", rowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }


        // Processes product updates, including replacing the product image via Azure Blob.
        [HttpPost]
        public async Task<IActionResult> Edit(Product updatedProduct, IFormFile? image)
        {
            var existing = await _productService.GetEntityAsync("Product", updatedProduct.RowKey);
            if (existing == null)
                return NotFound();

            existing.ProductName = updatedProduct.ProductName;
            existing.Category = updatedProduct.Category;
            existing.Description = updatedProduct.Description;
            existing.Price = updatedProduct.Price;
            existing.CreatedDate = DateTime.SpecifyKind(existing.CreatedDate, DateTimeKind.Utc);
            existing.StockQuantity = updatedProduct.StockQuantity;

            if (existing.CreatedDate == default)
                existing.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);


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

            await _productService.UpdateEntityAsync(existing, existing.PartitionKey, existing.RowKey);


            return RedirectToAction("Index");
        }


        // Deletes a product from Azure Table Storage.
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _productService.GetEntityAsync("Product", id);
            if (existing == null)
                return NotFound();

            await _productService.DeleteEntityAsync("Product", id);
            return RedirectToAction("Index");
        }
    }
}