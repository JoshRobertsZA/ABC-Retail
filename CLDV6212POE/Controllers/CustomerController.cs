using CLDV6212POE.Data;
using CLDV6212POE.Models;
using CLDV6212POE.Services;
using CLDV6212POE.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLDV6212POE.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService<Customer> _customerService;
        private readonly FunctionConnector _functionConnector;
        private readonly AppDbContext _context;

        public CustomerController(TableStorageService<Customer> customerService,
            FunctionConnector functionConnector, AppDbContext context)
        {
            _customerService = customerService;
            _functionConnector = functionConnector;
            _context = context;
        }


        // Displays a list of all customers
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
                return Unauthorized();

            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null) return NotFound("Customer role not found");

            var userList = await _context.UserRoles
                .Where(ur => ur.RoleId == customerRole.RoleId)
                .Include(ur => ur.User)
                .Select(ur => ur.User)
                .ToListAsync();

            var customers = new List<CustomerViewModel>();

            foreach (var user in userList)
            {
                // Fetch Azure Table Customer entity
                var customerEntity = await _customerService.GetEntityAsync("Customer", user.UserId.ToString());

                customers.Add(new CustomerViewModel
                {
                    UserId = user.UserId,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    PartitionKey = "Customer",
                    RowKey = user.UserId.ToString(),
                    CreatedDate = customerEntity?.CreatedDate ?? DateTimeOffset.MinValue
                });
            }

            return View(customers);
        }


        // Displays all customer-related queue messages (Admin only)
        public async Task<IActionResult> ViewCustomerQueue()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
                return Unauthorized();

            var messages = await _functionConnector.ReceiveMessagesAsync();
            return View(messages);
        }


        // Deletes a customer from both Azure Table Storage and SQL Database (Admin only)
        [HttpGet]
        public async Task<IActionResult> Delete(string customerPartitionKey)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
                return Unauthorized();

            if (string.IsNullOrEmpty(customerPartitionKey))
                return BadRequest("CustomerPartitionKey is required");

            // Get customer from Azure Table Storage using PartitionKey
            var customer = await _customerService.GetCustomerByUserAsync(customerPartitionKey);
            if (customer != null)
            {
                await _customerService.DeleteEntityAsync(customer.PartitionKey, customer.RowKey);
            }

            // Delete User from SQL Database
            if (Guid.TryParse(customerPartitionKey, out Guid userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

    }
}

