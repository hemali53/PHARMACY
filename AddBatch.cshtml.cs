using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System;
using System.Threading.Tasks;

namespace PHARMACY.Pages.Medicines
{
    public class AddBatchModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddBatchModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int MedicineID { get; set; }

        public string MedicineName { get; set; } = "Unknown Medicine";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                MedicineID = id;

                // Get medicine name with NULL handling
                var medicine = await _context.Medicines
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MedicineID == id);

                if (medicine != null)
                {
                    MedicineName = medicine.Name ?? "Unknown Medicine Name";
                }
                else
                {
                    // If medicine not found, redirect to medicines list
                    TempData["ErrorMessage"] = "Medicine not found!";
                    return RedirectToPage("/Medicines/Index");
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Log error and show friendly message
                Console.WriteLine($"Error in AddBatch OnGet: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading medicine details. Please try again.";
                return RedirectToPage("/Medicines/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(string batchNumber, int quantity, decimal purchasePrice,
                                                  decimal sellingPrice, DateTime? manufactureDate, DateTime? expiryDate)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(batchNumber))
                {
                    TempData["ErrorMessage"] = "Batch number is required!";
                    return await ReloadPageAsync();
                }

                if (quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Quantity must be greater than 0!";
                    return await ReloadPageAsync();
                }

                if (purchasePrice <= 0 || sellingPrice <= 0)
                {
                    TempData["ErrorMessage"] = "Prices must be greater than 0!";
                    return await ReloadPageAsync();
                }

                // Check if medicine exists
                var medicineExists = await _context.Medicines
                    .AnyAsync(m => m.MedicineID == MedicineID);

                if (!medicineExists)
                {
                    TempData["ErrorMessage"] = "Medicine not found!";
                    return RedirectToPage("/Medicines/Index");
                }

                // Check if batch number already exists for this medicine
                var batchExists = await _context.MedicineBatches
                    .AnyAsync(b => b.MedicineID == MedicineID && b.BatchNumber == batchNumber);

                if (batchExists)
                {
                    TempData["ErrorMessage"] = $"Batch '{batchNumber}' already exists for this medicine!";
                    return await ReloadPageAsync();
                }

                // Create new batch
                var batch = new MedicineBatch
                {
                    MedicineID = MedicineID,
                    BatchNumber = batchNumber.Trim(),
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    SellingPrice = sellingPrice,
                    ManufactureDate = manufactureDate,
                    ExpiryDate = expiryDate,
                    CreatedDate = DateTime.Now
                };

                _context.MedicineBatches.Add(batch);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Batch '{batchNumber}' added successfully!";
                return RedirectToPage("/Medicines/BatchList", new { id = MedicineID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddBatch OnPost: {ex.Message}");
                TempData["ErrorMessage"] = $"Error adding batch: {ex.Message}";
                return await ReloadPageAsync();
            }
        }

        private async Task<IActionResult> ReloadPageAsync()
        {
            // Reload medicine details for the page
            var medicine = await _context.Medicines
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MedicineID == MedicineID);

            if (medicine != null)
            {
                MedicineName = medicine.Name ?? "Unknown Medicine Name";
            }

            return Page();
        }
    }
}