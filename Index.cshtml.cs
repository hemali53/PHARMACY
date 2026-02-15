using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PHARMACY.Pages.Inventory
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<StockUpdate> RecentStockUpdates { get; set; } = new List<StockUpdate>();
        public List<StockAlert> CriticalAlerts { get; set; } = new List<StockAlert>();

        public async Task OnGetAsync()
        {
            try
            {
                // Get all medicine batches with medicine details - WITH NULL HANDLING
                var batches = await _context.MedicineBatches
                    .Include(m => m.Medicine)
                    .Where(b => b.Medicine.IsActive)
                    .Where(b => b.PurchasePrice != null)  
                    .Where(b => b.SellingPrice != null)   
                    .Where(b => b.Quantity != null)       
                    .ToListAsync();

                // Calculate statistics with NULL handling
                TotalItems = batches.Sum(b => b.Quantity);

                // Handle NULL MinimumStockLevel - use default value if NULL
                LowStockCount = batches.Count(b =>
                {
                    var minStock = b.Medicine.MinimumStockLevel;
                    var actualMinStock = minStock.HasValue ? minStock.Value : 10; // Default to 10 if NULL
                    return b.Quantity <= actualMinStock && b.Quantity > 0;
                });

                OutOfStockCount = batches.Count(b => b.Quantity == 0);

                // Expiring in next 30 days - FIXED with proper date comparison
                var today = DateTime.Today;
                var expiryThreshold = today.AddDays(30);
                ExpiringSoonCount = batches.Count(b => b.ExpiryDate.HasValue &&
                                                    b.ExpiryDate.Value >= today &&
                                                    b.ExpiryDate.Value <= expiryThreshold);

                // Recent stock updates (last 7 days)
                RecentStockUpdates = batches
                    .Where(b => b.CreatedDate >= DateTime.Now.AddDays(-7))
                    .OrderByDescending(b => b.CreatedDate)
                    .Take(5)
                    .Select(b => new StockUpdate
                    {
                        MedicineName = b.Medicine.Name,
                        BatchNumber = b.BatchNumber,
                        Quantity = b.Quantity,
                        UpdateDate = b.CreatedDate
                    })
                    .ToList();

                // Critical alerts with NULL handling
                CriticalAlerts = batches
                    .Where(b =>
                    {
                        var minStock = b.Medicine.MinimumStockLevel;
                        var actualMinStock = minStock.HasValue ? minStock.Value : 10;

                        return (b.Quantity <= actualMinStock && b.Quantity > 0) ||
                               (b.ExpiryDate.HasValue && b.ExpiryDate.Value <= DateTime.Now.AddDays(30) && b.ExpiryDate.Value >= DateTime.Today);
                    })
                    .OrderBy(b => b.ExpiryDate) // Sort by most urgent first
                    .ThenBy(b => b.Quantity)    // Then by stock level
                    .Select(b =>
                    {
                        var minStock = b.Medicine.MinimumStockLevel;
                        var actualMinStock = minStock.HasValue ? minStock.Value : 10;
                        var isExpiringSoon = b.ExpiryDate.HasValue &&
                                           b.ExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                                           b.ExpiryDate.Value >= DateTime.Today;

                        string alertType;
                        if (b.Quantity == 0)
                            alertType = "Out of Stock";
                        else if (b.Quantity <= actualMinStock && isExpiringSoon)
                            alertType = "Critical: Low Stock & Expiring";
                        else if (b.Quantity <= actualMinStock)
                            alertType = "Low Stock";
                        else if (isExpiringSoon)
                            alertType = "Expiring Soon";
                        else
                            alertType = "Normal";

                        return new StockAlert
                        {
                            MedicineName = b.Medicine.Name,
                            CurrentStock = b.Quantity,
                            MinimumStock = actualMinStock,
                            ExpiryDate = b.ExpiryDate,
                            AlertType = alertType,
                            BatchNumber = b.BatchNumber,
                            DaysUntilExpiry = b.ExpiryDate.HasValue ? (b.ExpiryDate.Value - DateTime.Today).Days : (int?)null
                        };
                    })
                    .Where(alert => alert.AlertType != "Normal") // Exclude normal items
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Error handling
                TotalItems = 0;
                LowStockCount = 0;
                ExpiringSoonCount = 0;
                OutOfStockCount = 0;
                RecentStockUpdates = new List<StockUpdate>();
                CriticalAlerts = new List<StockAlert>();

                // Optional: Log the error
                Console.WriteLine($"Error in Inventory Index: {ex.Message}");
            }
        }
    }

    public class StockUpdate
    {
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public int Quantity { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public class StockAlert
    {
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string AlertType { get; set; }
        public int? DaysUntilExpiry { get; set; }
    }
}