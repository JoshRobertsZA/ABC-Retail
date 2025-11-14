using Azure;
using CLDV6212POE.Models;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;

public class CartController : Controller
{
    private readonly CartService _cartService;
    private readonly TableStorageService<Customer> _customerService;

    public CartController(CartService cartService, TableStorageService<Customer> customerService)
    {
        _cartService = cartService;
        _customerService = customerService;

    }

    // Displays the logged-in user's cart items from Azure Table Storage.
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var customer = await _customerService.GetCustomerByUserAsync(userId);
        if (customer == null)
            return View(new List<Cart>());

        var cartItems = await _cartService.GetAllAsync(customer.RowKey);
        return View(cartItems);
    }


    // Adds a product to the logged-in user's cart and stores it in Azure Table Storage.
    [HttpPost]
    public async Task<IActionResult> AddToCart(string productId, string productName, string imageUrl, decimal price)
    {
        var userId = HttpContext.Session.GetString("UserId");

        var userEmail = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userEmail))
            return Json(new { success = false, message = "You must be logged in to add items." });

        // Retrieve the current customer from storage
        var customer = await _customerService.GetCustomerByUserAsync(userId);
        if (customer == null)
            return Json(new { success = false, message = "Customer not found." });

        var cartItem = new Cart
        {
            PartitionKey = customer.RowKey,
            RowKey = Guid.NewGuid().ToString(),
            ProductId = productId,
            ProductName = productName,
            ImageUrl = imageUrl,
            ProductPrice = price,
            Quantity = 1
        };

        await _cartService.AddToCartAsync(cartItem);
        return Json(new { success = true });
    }


    // Returns all cart items for the logged-in user as JSON (for AJAX updates).
    [HttpGet]
    public async Task<IActionResult> GetCartItems()
    {
        var userId = HttpContext.Session.GetString("UserId");

        var userEmail = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userEmail))
            return Json(new List<Cart>());

        var customer = await _customerService.GetCustomerByUserAsync(userId);
        if (customer == null)
            return Json(new List<Cart>());

        var items = await _cartService.GetUserCartAsync(customer.RowKey);
        return Json(items);
    }


    // Removes a specific product from the user's cart using PartitionKey + RowKey.
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(string productId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "User not logged in." });

        var customer = await _customerService.GetCustomerByUserAsync(userId);
        if (customer == null)
            return Json(new { success = false, message = "Customer not found." });

        try
        {
            // Get the cart item by ProductId
            var cartItems = await _cartService.GetUserCartAsync(customer.RowKey);
            var cartItem = cartItems.FirstOrDefault(c => c.ProductId == productId);

            if (cartItem == null)
                return Json(new { success = false, message = "Item not found in cart." });

            // Now remove using PartitionKey + RowKey
            await _cartService.RemoveFromCartAsync(cartItem.PartitionKey, cartItem.RowKey);

            return Json(new { success = true });
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return Json(new { success = false, message = "Item not found in cart." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


