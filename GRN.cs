using Microsoft.AspNetCore.Mvc.Rendering;

public class GRN
{
    public int Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Location { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public bool VATApplied { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<GRNItem> Items { get; set; } = new();


}
