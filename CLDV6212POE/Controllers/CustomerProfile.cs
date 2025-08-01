using Azure.Data.Tables;
using CLDV6212POE.Models.Entities;
using CLDV6212POE.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CLDV6212POE.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService<CustomerProfile> _tableStorage;

        // Injected service for Azure Table Storage operations
        public CustomerController(TableStorageService<CustomerProfile> tableStorage)
        {
            _tableStorage = tableStorage;
        }


        // Displays a list of all customers
        public async Task<IActionResult> IndexAsync()
        {
            var customers = await _tableStorage.GetAllEntitiesAsync();
            return View(customers);
        }


        // Shows the form to add a new customer
        [HttpGet]
        public IActionResult Add()
        {
            return View(new CustomerProfile());
        }


        // Shows the form to add a new customer
        [HttpPost]
        public async Task<IActionResult> Add(CustomerProfile customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.DateOfBirth = DateTime.SpecifyKind(customer.DateOfBirth.DateTime, DateTimeKind.Utc);


            await _tableStorage.AddEntityAsync(customer);
            return RedirectToAction("Index");
        }


        // Loads the edit form with existing customer data
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _tableStorage.GetEntityAsync(partitionKey, rowKey);
            if (customer == null)
                return NotFound();

            return View(customer);
        }


        // Saves changes made to an existing 
        [HttpPost]
        public async Task<IActionResult> Edit(CustomerProfile updatedCustomer)
        {
            var existing = await _tableStorage.GetEntityAsync("Customer", updatedCustomer.RowKey);
            if (existing == null)
                return NotFound();

            existing.FullName = updatedCustomer.FullName;
            existing.Email = updatedCustomer.Email;
            existing.PhoneNumber = updatedCustomer.PhoneNumber;
            existing.Address = updatedCustomer.Address;

            existing.DateOfBirth = DateTime.SpecifyKind(updatedCustomer.DateOfBirth.DateTime, DateTimeKind.Utc);

            await _tableStorage.UpdateEntityAsync(existing);
            return RedirectToAction("Index");
        }


        // Deletes a customer by partition and row key
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var existing = await _tableStorage.GetEntityAsync(partitionKey, rowKey);
            if (existing == null)
                return NotFound();

            await _tableStorage.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }
    }
}
