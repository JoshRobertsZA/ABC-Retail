using CLDV6212POE.Data;
using CLDV6212POE.Models;
using CLDV6212POE.Models.Account;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace CLDV6212POE.Controllers
{
    public class AccountController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly FunctionConnector _functionConnector;
        private readonly EncryptionService _encryptionService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, IConfiguration config, FunctionConnector functionConnector,
            EncryptionService encryptionService, ILogger<AccountController> logger)
        {
            _context = context;
            _config = config;
            _functionConnector = functionConnector;
            _encryptionService = encryptionService;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid login request." });

            // Fetch user by email only
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
                return Json(new { success = false, message = "Invalid email or password." });

            // Decrypt the stored password
            var encryptedBytes = Convert.FromBase64String(user.Password);
            var decryptedBytes = _encryptionService.Decrypt(encryptedBytes);
            var decryptedPassword = System.Text.Encoding.UTF8.GetString(decryptedBytes);

            // Compare decrypted password with the input
            if (decryptedPassword != model.Password)
                return Json(new { success = false, message = "Invalid email or password." });

            // Get user role via UserRoles table
            var userRole = (from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.RoleId
                where ur.UserId == user.UserId
                select r.RoleName).FirstOrDefault();

            // Save session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("UserName", user.Fullname);
            HttpContext.Session.SetString("UserRole", userRole ?? "Customer");

            // Set TempData alert
            TempData["Alert"] = JsonSerializer.Serialize(new CLDV6212POE.ViewModel.AlertViewModel
            {
                Title = $"Welcome back {user.Fullname}!",
                Message = "You’ve successfully logged in.",
                Type = "success"
            });

            // Determine redirect
            string redirectUrl = model.ReturnUrl;
            if (string.IsNullOrEmpty(redirectUrl) || redirectUrl.Contains("/Account"))
            {
                redirectUrl = Url.Action("Index", "Home");
            }

            return Json(new
            {
                success = true,
                message = "Login successful!",
                role = userRole,
                redirectUrl
            });
        }


        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            // Check if user already exists in SQL
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                return Json(new { success = false, message = "Email already registered." });
            }

            // Convert password string to bytes
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(model.Password);

            // Encrypt password
            var encryptedBytes = _encryptionService.Encrypt(passwordBytes);

            // Convert to Base64 string to store in DB
            var encryptedPassword = Convert.ToBase64String(encryptedBytes);

            // Create SQL user
            var user = new User
            {
                Fullname = model.Name,
                Email = model.Email,
                Password = encryptedPassword
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Assign "Customer" role
            var customerRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Customer");
            if (customerRole != null)
            {
                var userRole = new UserRole
                {
                    UserRoleId = Guid.NewGuid(),
                    UserId = user.UserId,
                    RoleId = customerRole.RoleId
                };
                _context.UserRoles.Add(userRole);
                _context.SaveChanges();
            }

            // Prepare the customer object to send to the function
            var safeUserId = user.UserId.ToString()
                .Replace("/", "_").Replace("\\", "_").Replace("#", "_")
                .Replace("?", "_").Replace("[", "_").Replace("]", "_");

            var customer = new Customer
            {
                PartitionKey = safeUserId,
                RowKey = Guid.NewGuid().ToString(),
                FullName = user.Fullname,
                Email = user.Email,
                CreatedDate = DateTimeOffset.UtcNow
            };

            // Send to the function for storage
            var storedSuccessfully = await _functionConnector.StoreCustomerAsync(customer);
            if (!storedSuccessfully)
            {
                _logger.LogWarning("Customer {Email} could not be stored in Table Storage.", user.Email);
            }

            // Create session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("UserName", user.Fullname);
            HttpContext.Session.SetString("UserRole", "Customer");

            TempData["Alert"] = JsonSerializer.Serialize(new CLDV6212POE.ViewModel.AlertViewModel
            {
                Title = $"You are registered!",
                Message = "You’ve successfully registered",
                Type = "success"
            });

            return Json(new { success = true, message = "Registration successful!" });
        }


        public class RegisterRequest
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }


        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string ReturnUrl { get; set; }
        }


        [HttpPost]
        public IActionResult Logout()
        {
            TempData["Alert"] = JsonSerializer.Serialize(new CLDV6212POE.ViewModel.AlertViewModel
            {
                Title = "Logout Successful",
                Message = $"See you soon",
                Type = "success"
            });
            HttpContext.Session.Clear();
            return Ok();
        }

    }
}
