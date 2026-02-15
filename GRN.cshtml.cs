using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using PHARMACY.Data;
using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Pages
{
    public class GRNModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public GRNModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // DTO for Supplier Data
        public class SupplierDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        // DTO for Medicine Data
        public class MedicineDto
        {
            public int MedicineID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal? AverageCost { get; set; }
            public bool IsVat { get; set; }
            public List<BatchDto> Batches { get; set; } = new();
        }

        public class BatchDto
        {
            public int BatchID { get; set; }
            public string BatchNumber { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SellingPrice { get; set; }
        }

        // Properties
        public List<SupplierDto> AllSuppliers { get; set; } = new();
        public List<SelectListItem> SupplierSelectList { get; set; } = new();
        public List<MedicineDto> AllMedicines { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public string ItemsJson { get; set; } = string.Empty;

        public class InputModel
        {
            public string? Location { get; set; }
            public string? InvoiceNumber { get; set; }
            public int? SupplierId { get; set; }
            public string? SupplierAddress { get; set; }
            public string? SupplierPhone { get; set; }
            public bool VAT { get; set; }
        }

        public class ItemDto
        {
            public string ProductName { get; set; } = string.Empty;
            public int? MedicineID { get; set; } 
            public string? BatchNumber { get; set; } 
            public decimal Quantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal LineTotal { get; set; }
        }

        // LOAD DATA
        public void OnGet()
        {
            // Load Suppliers
            AllSuppliers = _db.Suppliers
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address ?? string.Empty,
                    Phone = s.Phone ?? string.Empty
                })
                .ToList();

            SupplierSelectList = AllSuppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            // Load Medicines with Batches
            AllMedicines = _db.Medicines
                .Include(m => m.Batches)
                .Where(m => m.IsActive)
                .Select(m => new MedicineDto
                {
                    MedicineID = m.MedicineID,
                    Name = m.Name,
                    Description = m.Description ?? string.Empty,
                    AverageCost = m.AverageCost,
                    IsVat = m.IsVat,
                    Batches = m.Batches.Where(b => b.Quantity > 0)
                        .Select(b => new BatchDto
                        {
                            BatchID = b.BatchID,
                            BatchNumber = b.BatchNumber,
                            Quantity = b.Quantity,
                            PurchasePrice = b.PurchasePrice,
                            SellingPrice = b.SellingPrice
                        }).ToList()
                })
                .ToList();
        }

        // SAVE GRN - UPDATED FOR NULLABLE TYPES
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                OnGet();

                if (string.IsNullOrWhiteSpace(ItemsJson))
                {
                    ModelState.AddModelError(string.Empty, "No items provided.");
                    return Page();
                }

                var items = JsonConvert.DeserializeObject<List<ItemDto>>(ItemsJson) ?? new();

                if (items.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please add at least one item.");
                    return Page();
                }

                // Calculate totals
                decimal invoiceTotal = 0m;
                decimal discountTotal = 0m;

                foreach (var it in items)
                {
                    decimal line = it.Quantity * it.PurchasePrice;
                    decimal disc = line * (it.DiscountPercent / 100m);
                    discountTotal += disc;
                    invoiceTotal += (line - disc);
                }

                decimal vatRate = Input.VAT ? 0.12m : 0m;
                decimal vatAmount = invoiceTotal * vatRate;
                decimal grandTotal = invoiceTotal + vatAmount;

                // Create main GRN record
                var grn = new GRN
                {
                    InvoiceNumber = Input.InvoiceNumber ?? string.Empty,
                    Location = Input.Location ?? string.Empty,
                    SupplierId = Input.SupplierId,
                    InvoiceTotal = invoiceTotal,
                    TotalDiscount = discountTotal,
                    VATApplied = Input.VAT,
                    GrandTotal = grandTotal,
                    CreatedAt = DateTime.UtcNow
                };

                _db.GRNs.Add(grn);
                await _db.SaveChangesAsync();

                // Insert GRN Items
                foreach (var it in items)
                {
                    var grnItem = new GRNItem
                    {
                        GRNId = grn.Id,
                        ProductName = it.ProductName,
                        MedicineID = it.MedicineID, 
                        BatchNumber = it.BatchNumber,
                        Quantity = it.Quantity,
                        PurchasePrice = it.PurchasePrice,
                        DiscountPercent = it.DiscountPercent,
                        LineTotal = it.LineTotal,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.GRNItems.Add(grnItem);

                    // Update stock only if MedicineID and BatchNumber are provided
                    if (it.MedicineID.HasValue && it.MedicineID > 0 && !string.IsNullOrEmpty(it.BatchNumber))
                    {
                        var existingBatch = await _db.MedicineBatches
                            .FirstOrDefaultAsync(b => b.MedicineID == it.MedicineID.Value && b.BatchNumber == it.BatchNumber);

                        if (existingBatch != null)
                        {
                            existingBatch.Quantity += (int)it.Quantity;
                        }
                    }
                }

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"GRN saved successfully (ID = {grn.Id}). Stock updated.";

                return RedirectToPage("/GRN");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving GRN: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"An error occurred while saving the GRN: {ex.Message}");
                OnGet();
                return Page();
            }
        }
    }
}