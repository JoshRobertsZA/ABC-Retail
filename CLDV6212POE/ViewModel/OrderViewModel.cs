using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

public class OrderViewModel
{
    public string CustomerId { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }

    // Dropdowns
    public IEnumerable<SelectListItem> Customers { get; set; }
    public IEnumerable<SelectListItem> Products { get; set; }
}