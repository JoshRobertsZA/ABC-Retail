using Azure;
using Azure.Data.Tables;
using CLDV6212POE.Models;
using CLDV6212POE.Services;
using CLDV6212POE.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace CLDV6212POE.Controllers
{
    public class OrderController : Controller
    {
        private readonly string _connectionString;
        private readonly string _queueName = "order-queue";
        private readonly TableStorageService<Customer> _customerService;
        private readonly TableStorageService<Product> _productService;
        private readonly TableStorageService<Order> _orderService;
        private readonly CartService _cartService;
        private readonly FunctionConnector _functionConnector;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            TableStorageService<Customer> customerService,
            TableStorageService<Product> productService,
            TableStorageService<Order> orderService,
            CartService cartService,
            FunctionConnector functionConnector,
            ILogger<OrderController> logger)
        {
            _customerService = customerService;
            _productService = productService;
            _orderService = orderService;
            _functionConnector = functionConnector;
            _logger = logger;
            _cartService = cartService;
        }

        // Displays all orders. Admin sees all orders, regular users only see their own.
        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetString("UserId");

            var orders = await _orderService.GetAllEntitiesAsync();
            var products = await _productService.GetAllEntitiesAsync();

            if (userRole != "Admin" && !string.IsNullOrEmpty(userId))
            {
                // Only show logged-in user's orders
                orders = orders.Where(o => o.CustomerId == userId).ToList();
            }

            // Map Order → OrderViewModel
            var viewModel = orders.Select(o =>
            {
                var product = products.FirstOrDefault(p => p.RowKey == o.ProductId);

                return new OrderViewModel
                {
                    CustomerName = o.CustomerName,
                    CustomerId = o.CustomerId,
                    ProductName = o.ProductName,
                    ProductID = o.ProductId,
                    Quantity = o.Quantity,
                    OrderDate = o.OrderDate.DateTime,
                    QueueStatus = o.QueueStatus,
                    RowKey = o.RowKey,

                    // ✅ Image & Price from Azure product data
                    ImageUrl = product?.ImageUrl ?? "/images/no-image.png",
                    Price = product?.Price
                };
            }).ToList();

            return View(viewModel);
        }


        // Creates orders from all items in the logged-in user's cart and queues them for processing.
        [HttpPost]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Identify the current user (assuming user is authenticated)
                var userId = HttpContext.Session.GetString("UserId");

                var customer = await _customerService.GetCustomerByUserAsync(userId);

                if (customer == null)
                {
                    TempData["Error"] = "No customer record found for this user.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get all items from this customer's cart
                var cartItems = await _cartService.GetUserCartAsync(customer.RowKey);

                if (cartItems == null || !cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                // Loop through each cart item → create an order message
                foreach (var item in cartItems)
                {
                    // Fetch product details for each cart item
                    var product = await _productService.GetEntityAsync("Product", item.ProductId);

                    if (product == null)
                        continue; // skip if product not found

                    var order = new Order
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = userId,
                        CustomerName = customer.FullName,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OrderDate = DateTimeOffset.UtcNow,
                        QueueStatus = "Pending"
                    };

                    // Send each order to your Azure Function queue
                    await _functionConnector.SendMessageAsync(order);
                }

                // Clear the cart
                await _cartService.ClearCartAsync(customer.RowKey);

                _logger.LogInformation("All cart items queued as orders successfully.");
                TempData["Success"] = "Your order has been placed successfully!";

                return View("Confirmation", new Order
                {
                    CustomerId = customer.RowKey,
                    CustomerName = customer.FullName,
                    OrderDate = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating orders from cart");
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }


        // Sends a queue message to delete a specific order entity from Azure Table Storage.
        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            string partitionKey = "Order"; // this matches your Order entity

            var order = await _orderService.GetEntityAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            var queueMessage = new QueueMessageViewModel
            {
                TableName = "Order",
                EntityId = rowKey,
                Action = "Delete",
                MessageText = $"Order deleted: {order.CustomerName} - {order.ProductName}"
            };

            await _functionConnector.SendMessageAsync(queueMessage);

            _logger.LogInformation("Queue message sent to delete order {OrderId}", rowKey);
            return RedirectToAction(nameof(Index));
        }


        // Updates the status of an order (Processing or Completed) and saves it back to Azure Table Storage.
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string rowKey, string newStatus)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(newStatus))
            {
                TempData["Error"] = "Invalid input.";
                return RedirectToAction(nameof(Index));
            }

            var allowedStatuses = new[] { "Processing", "Completed" };
            if (!allowedStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _orderService.GetEntityAsync("Order", rowKey);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                order.QueueStatus = newStatus;
                order.ETag = ETag.All; // overwrite any ETag

                await _orderService.UpdateEntityAsync(order, "Order", order.RowKey);

                TempData["Success"] = $"Order status updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}