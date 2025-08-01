using Azure.Data.Tables;
using CLDV6212POE.Models.Entities;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212POE.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService<ProductInfo> _tableStorage;
        private readonly QueueStorageService<ProductInfo> _queueService;

        public ProductController(TableStorageService<ProductInfo> tableStorage, QueueStorageService<ProductInfo> queueStorage)
        {
            _tableStorage = tableStorage;
            _queueService = queueStorage;
        }


        public async Task<IActionResult> Index()
        {
            var products = (await _tableStorage.GetAllEntitiesAsync()).ToList();
            return View(products);
        }


        [HttpGet]
        public IActionResult Add()
        {
            return View(new ProductInfo());
        }


        [HttpPost]
        public async Task<IActionResult> Add(string productName, string description, string price, string category,
            int stockQuantity, IFormFile image, [FromServices] BlobStorageService blobService)
        {
            string? imageUrl = null;

            if (image != null && image.Length > 0)
            {
                imageUrl = await blobService.UploadFileAsync(image);
            }

            var newProduct = new ProductInfo
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

            await _queueService.SendMessageAsync($"Uploading image: {imageUrl}");
            await _queueService.SendMessageAsync($"New product added: {productName} at {DateTime.UtcNow}");

            await _tableStorage.AddEntityAsync(newProduct);

            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            var product = await _tableStorage.GetEntityAsync("Product", rowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(ProductInfo updatedProduct, IFormFile? image,
            [FromServices] BlobStorageService blobService)
        {
            var existing = await _tableStorage.GetEntityAsync("Product", updatedProduct.RowKey);
            if (existing == null)
                return NotFound();

            // Update fields
            existing.ProductName = updatedProduct.ProductName;
            existing.Description = updatedProduct.Description;
            existing.Price = updatedProduct.Price;
            existing.Category = updatedProduct.Category;
            existing.StockQuantity = updatedProduct.StockQuantity;

            if (image != null && image.Length > 0)
            {
                existing.ImageUrl = await blobService.UploadFileAsync(image);
            }

            await _queueService.SendMessageAsync($"Product: {existing.ProductName} has been altered");

            await _tableStorage.UpdateEntityAsync(existing);
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            var existing = await _tableStorage.GetEntityAsync("Product", rowKey);
            if (existing == null)
                return NotFound();

            await _queueService.SendMessageAsync($"Product: {existing.ProductName} has been deleted");

            await _tableStorage.DeleteEntityAsync("Product", rowKey);
            return RedirectToAction("Index");
        }
    }
}