using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Function.Models;

namespace CLDV6212POE.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService<Customer> _customerService;
        private readonly FunctionConnector _functionConnector;


        // Injected service for Azure Table Storage operations
        public CustomerController(TableStorageService<Customer> customerService,
            FunctionConnector functionConnector)
        {
            _customerService = customerService;
            _functionConnector = functionConnector;
        }


        // Displays a list of all customers
        public async Task<IActionResult> IndexAsync(string searchTerm)
        {
            var customers = await _customerService.GetAllEntitiesAsync();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                customers = customers
                    .Where(c => (c.FullName != null && c.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                (c.Email != null && c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return View(customers);
        }

        public async Task<IActionResult> ViewCustomerQueue()
        {
            var messages = await _functionConnector.ReceiveMessagesAsync();
            return View(messages);
        }

        // Shows the form to add a new customer
        [HttpGet]
        public IActionResult Add()
        {
            return View(new Customer());
        }


        // Shows the form to add a new customer
        [HttpPost]
        public async Task<IActionResult> Add(Customer customer)
        {
            customer.DateOfBirth = DateTime.SpecifyKind(customer.DateOfBirth.DateTime, DateTimeKind.Utc);

            await _functionConnector.SendCustomerMessageAsync(customer);

            var success = await _functionConnector.StoreCustomerAsync(customer);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to store order via Azure Function.");
                return View(customer);
            }


            return RedirectToAction("Index");
        }


        // Loads the edit form with existing customer data
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _customerService.GetEntityAsync(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }


        // Saves changes made to an existing 
        [HttpPost]
        public async Task<IActionResult> Edit(Customer updatedCustomer)
        {
            var existing = await _customerService.GetEntityAsync("Customer", updatedCustomer.RowKey);
            if (existing == null)
                return NotFound();

            existing.FullName = updatedCustomer.FullName;
            existing.Email = updatedCustomer.Email;
            existing.PhoneNumber = updatedCustomer.PhoneNumber;
            existing.Address = updatedCustomer.Address;

            existing.DateOfBirth = DateTime.SpecifyKind(updatedCustomer.DateOfBirth.DateTime, DateTimeKind.Utc);

            await _customerService.UpdateEntityAsync(existing);
            return RedirectToAction("Index");
        }


        // Deletes a customer by partition and row key
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var existing = await _customerService.GetEntityAsync(partitionKey, rowKey);
            if (existing == null)
                return NotFound();

            await _customerService.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }
    }
}
