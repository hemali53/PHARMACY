using PHARMACY.Models;

public class StockItem
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; }
    public string BatchNumber { get; set; }
    public int Quantity { get; set; }
    public int MinimumStock { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime LastUpdated { get; set; }
}