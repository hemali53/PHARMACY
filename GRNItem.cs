public class GRNItem
{
    public int Id { get; set; }
    public int GRNId { get; set; }
    public GRN? GRN { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FIXED: Make these nullable to match database
    public int? MedicineID { get; set; } // Changed to nullable int
    public string? BatchNumber { get; set; } // Changed to nullable string
}