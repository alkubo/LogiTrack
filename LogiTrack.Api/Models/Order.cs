using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Api.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DatePlaced { get; set; } = DateTime.UtcNow;

    // Navigation collection (one-to-many)
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();

    public void AddItem(InventoryItem item)
    {
        // Use dictionary-like lookup optimization: avoid multiple LINQ enumerations
        var existing = Items.FirstOrDefault(i => i.ItemId == item.ItemId);
        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            Items.Add(item);
        }
    }

    public void RemoveItem(int itemId)
    {
        var existing = Items.FirstOrDefault(i => i.ItemId == itemId);
        if (existing != null)
        {
            Items.Remove(existing);
        }
    }

    public string GetOrderSummary()
    {
        // Efficient summary formatting
        return $"Order #{OrderId} for {CustomerName} | Items: {Items.Count} | Placed: {DatePlaced:MM/dd/yyyy}";
    }
}
