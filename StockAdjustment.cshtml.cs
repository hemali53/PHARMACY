using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PHARMACY.Pages.Inventory
{
    public class StockAdjustmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StockAdjustmentModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StockAdjustmentInput Input { get; set; } = new StockAdjustmentInput();

        public List<SelectListItem> Medicines { get; set; } = new List<SelectListItem>();
        public List<RecentAdjustment> RecentAdjustments { get; set; } = new List<RecentAdjustment>();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            try
            {
                // Check if medicine exists
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedicineID == Input.MedicineID);

                if (medicine == null)
                {
                    ModelState.AddModelError("", "Selected medicine not found!");
                    await LoadDataAsync();
                    return Page();
                }

                // Check if batch exists
                var batch = await _context.MedicineBatches
                    .FirstOrDefaultAsync(b => b.MedicineID == Input.MedicineID && b.BatchNumber == Input.BatchNumber);

                if (batch == null)
                {
                    ModelState.AddModelError("", "Selected batch not found!");
                    await LoadDataAsync();
                    return Page();
                }

                // Check if sufficient stock available
                if (batch.Quantity < Input.Quantity)
                {
                    ModelState.AddModelError("", $"Insufficient stock! Available: {batch.Quantity}");
                    await LoadDataAsync();
                    return Page();
                }

                // Create stock adjustment record
                var adjustment = new StockAdjustment
                {
                    MedicineID = Input.MedicineID,
                    BatchNumber = Input.BatchNumber,
                    AdjustmentType = Input.AdjustmentType,
                    Quantity = Input.Quantity,
                    Reason = Input.Reason,
                    AdjustmentDate = Input.AdjustmentDate,
                    AdjustedBy = User.Identity?.Name ?? "System"
                };

                _context.StockAdjustments.Add(adjustment);

                // Update stock quantity in the batch
                batch.Quantity -= Input.Quantity;
                if (batch.Quantity < 0) batch.Quantity = 0;

                // Save changes to database
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Stock adjustment recorded successfully! {Input.Quantity} units adjusted.";

                // RELOAD THE DATA AFTER SUCCESSFUL SAVE
                await LoadDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving adjustment: {ex.Message}");
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<JsonResult> OnGetBatchesAsync(int medicineId)
        {
            try
            {
                var batches = await _context.MedicineBatches
                    .Where(b => b.MedicineID == medicineId && b.Quantity > 0)
                    .Select(b => new { b.BatchNumber })
                    .Distinct()
                    .ToListAsync();

                return new JsonResult(batches);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load active medicines for dropdown
                Medicines = await _context.Medicines
                    .Where(m => m.IsActive)
                    .Select(m => new SelectListItem
                    {
                        Value = m.MedicineID.ToString(),
                        Text = $"{m.Name} (ID: {m.MedicineID})"
                    })
                    .ToListAsync();

                // Load recent adjustments - FIXED CODE
                RecentAdjustments = await _context.StockAdjustments
                    .Include(sa => sa.Medicine)
                    .OrderByDescending(sa => sa.AdjustmentDate)
                    .Take(5)
                    .Select(sa => new RecentAdjustment
                    {
                        MedicineName = sa.Medicine.Name,
                        AdjustmentType = sa.AdjustmentType,
                        Quantity = sa.Quantity,
                        Reason = sa.Reason,
                        AdjustmentDate = sa.AdjustmentDate
                    })
                    .ToListAsync();

                // Debug information
                Console.WriteLine($"Loaded {RecentAdjustments.Count} recent adjustments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadDataAsync: {ex.Message}");

                // If error, initialize empty lists
                Medicines = new List<SelectListItem>();
                RecentAdjustments = new List<RecentAdjustment>();

                // Add a sample message for debugging
                RecentAdjustments.Add(new RecentAdjustment
                {
                    MedicineName = "Error loading adjustments",
                    AdjustmentType = "Error",
                    Quantity = 0,
                    Reason = ex.Message,
                    AdjustmentDate = DateTime.Now
                });
            }
        }
    }

    public class StockAdjustmentInput
    {
        [Required(ErrorMessage = "Please select a medicine")]
        [Display(Name = "Medicine")]
        public int MedicineID { get; set; }

        [Required(ErrorMessage = "Please select a batch number")]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select adjustment type")]
        [Display(Name = "Adjustment Type")]
        public string AdjustmentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Please enter reason")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select date")]
        [Display(Name = "Adjustment Date")]
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;
    }

    public class RecentAdjustment
    {
        public string MedicineName { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
    }
}