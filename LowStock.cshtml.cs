using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PHARMACY.Pages.Inventory
{
    public class LowStockModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LowStockModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LowStockItem> LowStockItems { get; set; } = new List<LowStockItem>();

        public async Task OnGetAsync()
        {
            try
            {
                // Method 1: Direct SQL query to avoid NULL issues
                var lowStockData = await GetLowStockDataAsync();
                LowStockItems = lowStockData;

                // If no data found, show empty state
                if (!LowStockItems.Any())
                {
                    Console.WriteLine("No low stock items found in database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LowStock page: {ex.Message}");
                LowStockItems = new List<LowStockItem>();

                // Don't add sample data in production
                // Let the empty state handle it
            }
        }

        private async Task<List<LowStockItem>> GetLowStockDataAsync()
        {
            try
            {
                // Safe query with explicit NULL handling
                var query = from mb in _context.MedicineBatches
                            join m in _context.Medicines on mb.MedicineID equals m.MedicineID
                            where m.IsActive
                               && mb.Quantity > 0
                               && (mb.PurchasePrice != null || mb.PurchasePrice == 0)
                               && (mb.SellingPrice != null || mb.SellingPrice == 0)
                            select new { mb, m };

                var batches = await query.ToListAsync();

                // Filter low stock items
                var lowStockItems = batches
                    .Where(x =>
                    {
                        var medicine = x.m;
                        var batch = x.mb;

                        var minStock = medicine.MinimumStockLevel;
                        var actualMinStock = minStock.HasValue ? minStock.Value : 10;

                        return batch.Quantity <= actualMinStock;
                    })
                    .OrderBy(x => x.mb.Quantity)
                    .ThenBy(x => x.m.Name)
                    .Select(x => new LowStockItem
                    {
                        MedicineID = x.m.MedicineID,
                        MedicineName = x.m.Name,
                        BatchNumber = x.mb.BatchNumber,
                        CurrentStock = x.mb.Quantity,
                        MinimumStock = x.m.MinimumStockLevel.HasValue ? x.m.MinimumStockLevel.Value : 10,
                        Difference = (x.m.MinimumStockLevel.HasValue ? x.m.MinimumStockLevel.Value : 10) - x.mb.Quantity
                    })
                    .ToList();

                return lowStockItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLowStockDataAsync: {ex.Message}");
                return new List<LowStockItem>();
            }
        }

        // Alternative method using raw SQL
        private async Task<List<LowStockItem>> GetLowStockDataRawSqlAsync()
        {
            try
            {
                var sql = @"
                SELECT 
                    m.MedicineID,
                    m.Name AS MedicineName,
                    mb.BatchNumber,
                    mb.Quantity AS CurrentStock,
                    COALESCE(m.MinimumStockLevel, 10) AS MinimumStock,
                    (COALESCE(m.MinimumStockLevel, 10) - mb.Quantity) AS Difference
                FROM Medicines m
                INNER JOIN MedicineBatches mb ON m.MedicineID = mb.MedicineID
                WHERE m.IsActive = 1 
                    AND mb.Quantity > 0
                    AND mb.Quantity <= COALESCE(m.MinimumStockLevel, 10)
                    AND mb.PurchasePrice IS NOT NULL
                    AND mb.SellingPrice IS NOT NULL
                ORDER BY Difference DESC";

                var lowStockItems = await _context.LowStockItems
                    .FromSqlRaw(sql)
                    .ToListAsync();

                return lowStockItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in raw SQL method: {ex.Message}");
                return new List<LowStockItem>();
            }
        }
    }

    public class LowStockItem
    {
        public int MedicineID { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public int Difference { get; set; }
    }
}