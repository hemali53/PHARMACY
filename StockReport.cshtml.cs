using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PHARMACY.Pages.Inventory
{
    public class StockReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StockReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StockReportItem> StockItems { get; set; } = new List<StockReportItem>();

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StockStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; }

        public int TotalItems { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalStockValue { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Use simple query without IsActive filter first
                var medicineBatches = await _context.MedicineBatches
                    .Include(mb => mb.Medicine)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {medicineBatches?.Count ?? 0} medicine batches");

                // If no batches found, show empty page
                if (medicineBatches == null || !medicineBatches.Any())
                {
                    Console.WriteLine("DEBUG: No medicine batches found in database");
                    StockItems = new List<StockReportItem>();
                    return;
                }

                // Convert to StockReportItem with safe handling
                var allStockItems = new List<StockReportItem>();

                foreach (var mb in medicineBatches)
                {
                    try
                    {
                        var stockItem = new StockReportItem
                        {
                            MedicineId = mb.MedicineID,
                            BatchId = mb.BatchID,
                            MedicineName = mb.Medicine?.Name ?? "Unknown Medicine",
                            Description = mb.Medicine?.Description ?? "",
                            BatchNumber = mb.BatchNumber ?? "N/A",
                            CurrentStock = mb.Quantity,
                            MinimumStock = mb.Medicine?.MinimumStockLevel ?? 10,
                            ExpiryDate = mb.ExpiryDate ?? DateTime.Now.AddYears(1),
                            ManufactureDate = mb.ManufactureDate,
                            PurchasePrice = mb.PurchasePrice,
                            Price = mb.SellingPrice
                        };

                        allStockItems.Add(stockItem);
                        Console.WriteLine($"DEBUG: Added - {stockItem.MedicineName}, Stock: {stockItem.CurrentStock}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Error processing batch {mb.BatchID}: {ex.Message}");
                        // Continue with next batch
                    }
                }

                Console.WriteLine($"DEBUG: Created {allStockItems.Count} stock items");

                // Apply filters
                var filteredItems = allStockItems;

                // Stock status filter
                if (!string.IsNullOrEmpty(StockStatus) && StockStatus != "All")
                {
                    filteredItems = StockStatus switch
                    {
                        "LowStock" => allStockItems.Where(s => s.CurrentStock > 0 && s.CurrentStock <= s.MinimumStock).ToList(),
                        "OutOfStock" => allStockItems.Where(s => s.CurrentStock == 0).ToList(),
                        "InStock" => allStockItems.Where(s => s.CurrentStock > s.MinimumStock).ToList(),
                        _ => allStockItems
                    };
                    Console.WriteLine($"DEBUG: After stock filter: {filteredItems.Count} items");
                }

                // Search filter
                if (!string.IsNullOrEmpty(SearchString))
                {
                    filteredItems = filteredItems.Where(s =>
                        (s.MedicineName ?? "").Contains(SearchString, StringComparison.OrdinalIgnoreCase) ||
                        (s.BatchNumber ?? "").Contains(SearchString, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                    Console.WriteLine($"DEBUG: After search filter: {filteredItems.Count} items");
                }

                // Apply sorting
                StockItems = (SortBy switch
                {
                    "Stock" => filteredItems.OrderBy(s => s.CurrentStock),
                    "Expiry" => filteredItems.OrderBy(s => s.ExpiryDate),
                    "Price" => filteredItems.OrderByDescending(s => s.Price),
                    _ => filteredItems.OrderBy(s => s.MedicineName)
                }).ToList();

                // Calculate statistics
                TotalItems = StockItems.Count;
                LowStockCount = allStockItems.Count(s => s.CurrentStock > 0 && s.CurrentStock <= s.MinimumStock);
                OutOfStockCount = allStockItems.Count(s => s.CurrentStock == 0);
                TotalStockValue = StockItems.Sum(s => s.StockValue);

                Console.WriteLine($"DEBUG: Final - {StockItems.Count} items, Total Value: Rs. {TotalStockValue:N2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in StockReport: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                StockItems = new List<StockReportItem>();
            }
        }
    }

    public class StockReportItem
    {
        public int MedicineId { get; set; }
        public int BatchId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; } = 10;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(1);
        public DateTime? ManufactureDate { get; set; }
        public decimal Price { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal StockValue => CurrentStock * Price;
        public decimal ProfitMargin => Price - PurchasePrice;
        public decimal TotalProfitMargin => ProfitMargin * CurrentStock;
    }
}