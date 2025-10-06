using Azure.Data.Tables;
using CLDV6212POE.Services;
using CLDV6212POE.ViewModel;
using Function.Models;
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
        private readonly FunctionConnector _functionConnector;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            TableStorageService<Customer> customerService,
            TableStorageService<Product> productService,
            TableStorageService<Order> orderService,
            FunctionConnector functionConnector,
            ILogger<OrderController> logger)
        {
            _customerService = customerService;
            _productService = productService;
            _orderService = orderService;
            _functionConnector = functionConnector;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllEntitiesAsync();
            return View(orders ?? new List<Order>());
        }


        public async Task<IActionResult> Create()
        {
            var customers = await _customerService.GetAllEntitiesAsync();
            var products = await _productService.GetAllEntitiesAsync();

            var model = new OrderViewModel
            {
                Customers = customers
                    .Select(c => new SelectListItem
                    {
                        Value = c.RowKey,
                        Text = c.FullName
                    }).ToList(),

                Products = products
                    .Select(p => new SelectListItem
                    {
                        Value = p.RowKey,
                        Text = p.ProductName
                    }).ToList()
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel model)
        {
            try
            {
                // Validate customer & product
                var customer = await _customerService.GetEntityAsync("Customer", model.CustomerId);
                var product = await _productService.GetEntityAsync("Product", model.ProductId);

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "Select a Customer");
                    await ReloadDropdowns(model);
                    return View(model);
                }

                if (product == null)
                {
                    ModelState.AddModelError(string.Empty, "Select a Product");
                    await ReloadDropdowns(model);
                    return View(model);
                }

                // Build order object (don't set RowKey here - let the function do it)
                var order = new Order
                {
                    CustomerId = customer.RowKey,
                    CustomerName = customer.FullName,
                    ProductId = product.RowKey,
                    ProductName = product.ProductName,
                    Quantity = model.Quantity,
                    OrderDate = DateTimeOffset.UtcNow
                };

                // Send order to queue - the Azure Function will save it to the table
                await _functionConnector.SendMessageAsync(order);

                _logger.LogInformation("Order queued successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating order");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await ReloadDropdowns(model);
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            //var order = await _orderService.GetEntityAsync("Order", rowKey);
            //if (order == null) return NotFound();

            //// Only send delete message to queue; function will delete from table
            //var queueMessage = new QueueMessageViewModel
            //{
            //    TableName = "Order",
            //    EntityId = rowKey,
            //    Action = "Delete",
            //    MessageText = $"Order deleted: {order.CustomerName} - {order.ProductName}"
            //};

            //await _functionConnector.SendMessageAsync(queueMessage);

            //_logger.LogInformation("Queue message sent to delete order {OrderId}", rowKey);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string rowKey, string newStatus)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(newStatus))
            {
                TempData["Error"] = "Invalid input.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Use your TableStorageService or directly TableClient
                var order = await _orderService.GetEntityAsync("Order", rowKey);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                order.QueueStatus = newStatus;
                await _orderService.UpdateEntityAsync(order);

                TempData["Success"] = $"Order status updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        private async Task ReloadDropdowns(OrderViewModel model)
        {
            var customers = await _customerService.GetAllEntitiesAsync();
            var products = await _productService.GetAllEntitiesAsync();
            model.Customers = customers.Select(c => new SelectListItem { Value = c.RowKey, Text = c.FullName }).ToList();
            model.Products = products.Select(p => new SelectListItem { Value = p.RowKey, Text = p.ProductName }).ToList();
        }
    }
}
