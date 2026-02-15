using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PHARMACY.Pages.Inventory
{
    public class ExpiringSoonModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExpiringSoonModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ExpiringMedicine> ExpiringMedicines { get; set; } = new List<ExpiringMedicine>();

        [BindProperty(SupportsGet = true)]
        public string TimeFrame { get; set; } = "3months";

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public int ExpiringIn3Months { get; set; }
        public int ExpiringIn6Months { get; set; }
        public int TotalExpiring { get; set; }

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;
            DateTime thresholdDate;

            // Set threshold date based on selected timeframe
            if (TimeFrame == "6months")
            {
                thresholdDate = today.AddMonths(6);
            }
            else
            {
                thresholdDate = today.AddMonths(3);
            }

            // Get ALL batches that will expire in the future (not just within selected timeframe)
            var allFutureBatches = await _context.MedicineBatches
                .Include(mb => mb.Medicine)
                .Where(mb => mb.ExpiryDate.HasValue &&
                            mb.ExpiryDate >= today &&  // Future expiry dates only
                            mb.Quantity > 0)
                .ToListAsync();

            // Convert to ExpiringMedicine list
            ExpiringMedicines = allFutureBatches.Select(mb => new ExpiringMedicine
            {
                MedicineId = mb.MedicineID,
                BatchId = mb.BatchID,
                MedicineName = mb.Medicine?.Name ?? "Unknown",
                Description = mb.Medicine?.Description ?? string.Empty,
                BatchNumber = mb.BatchNumber,
                CurrentStock = mb.Quantity,
                ExpiryDate = mb.ExpiryDate.Value,
                DaysUntilExpiry = (mb.ExpiryDate.Value - today).Days,
                ExpiryCategory = GetExpiryCategory(mb.ExpiryDate.Value, today),
                PurchasePrice = mb.PurchasePrice,
                SellingPrice = mb.SellingPrice,
                StockValue = mb.Quantity * mb.SellingPrice
            }).ToList();

            // Apply timeframe filter - IMPORTANT FIX!
            if (TimeFrame == "6months")
            {
                ExpiringMedicines = ExpiringMedicines.Where(x => x.DaysUntilExpiry <= 180).ToList();
            }
            else // 3 months
            {
                ExpiringMedicines = ExpiringMedicines.Where(x => x.DaysUntilExpiry <= 90).ToList();
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(SearchString))
            {
                ExpiringMedicines = ExpiringMedicines.Where(s =>
                    s.MedicineName.Contains(SearchString, System.StringComparison.OrdinalIgnoreCase) ||
                    s.BatchNumber.Contains(SearchString, System.StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Sort by days until expiry
            ExpiringMedicines = ExpiringMedicines
                .OrderBy(x => x.DaysUntilExpiry)
                .ThenBy(x => x.MedicineName)
                .ToList();

            // Calculate statistics - FIXED!
            // These counts should be based on ALL data, not just filtered data
            var allExpiringMedicines = allFutureBatches.Select(mb => new
            {
                DaysUntilExpiry = (mb.ExpiryDate.Value - today).Days
            }).ToList();

            ExpiringIn3Months = allExpiringMedicines.Count(x => x.DaysUntilExpiry <= 90);
            ExpiringIn6Months = allExpiringMedicines.Count(x => x.DaysUntilExpiry <= 180);
            TotalExpiring = ExpiringMedicines.Count; // This should be the filtered count

        }

        private string GetExpiryCategory(DateTime expiryDate, DateTime today)
        {
            int daysUntilExpiry = (expiryDate - today).Days;

            if (daysUntilExpiry <= 90)
            {
                return "3 Months";
            }
            else if (daysUntilExpiry <= 180)
            {
                return "6 Months";
            }
            else
            {
                return "More than 6 Months";
            }
        }
    }

    public class ExpiringMedicine
    {
        public int MedicineId { get; set; }
        public int BatchId { get; set; }
        public string MedicineName { get; set; }
        public string Description { get; set; }
        public string BatchNumber { get; set; }
        public int CurrentStock { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string ExpiryCategory { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal StockValue { get; set; }
    }
}