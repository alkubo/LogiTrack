using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Api.Models;

public class InventoryItem
{
    [Key]
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;

    // One-to-many relationship: each InventoryItem can belong to one Order (optional for stock items)
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public void DisplayInfo()
    {
        Console.WriteLine($"Item: {Name} | Quantity: {Quantity} | Location: {Location}");
    }
}
