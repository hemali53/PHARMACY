//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using PHARMACY.Data;
//using PHARMACY.Models;
//using System;
//using System.Diagnostics;

//namespace PHARMACY.Pages
//{
//    public class PRNcreateModel : PageModel
//    {
//        private readonly ApplicationDbContext _db;

//        public PRNcreateModel(ApplicationDbContext db)
//        {
//            _db = db;
//        }

//        [BindProperty]
//        public PRN PRN { get; set; } = new PRN();

//        [BindProperty]
//        public string ItemsJson { get; set; } = string.Empty;

//        public IEnumerable<SelectListItem> SupplierList { get; set; } = new List<SelectListItem>();
//        public List<Medicine> MedicineList { get; set; } = new();

//        // Add property for success message
//        [TempData]
//        public string SuccessMessage { get; set; } = string.Empty;

//        public async Task OnGet()
//        {
//            await PopulateDropdowns();

//            // Auto-generate return number
//            PRN.ReturnNo = await GenerateReturnNumber();

//            // Set default date to today
//            PRN.ReturnDate = DateTime.Today;
//        }

//        private async Task PopulateDropdowns()
//        {
//            SupplierList = await _db.Suppliers
//                .Select(s => new SelectListItem
//                {
//                    Value = s.Id.ToString(),
//                    Text = s.Name
//                })
//                .ToListAsync();

//            MedicineList = await _db.Medicines.ToListAsync();
//        }

//        private async Task<string> GenerateReturnNumber()
//        {
//            try
//            {
//                var prnCount = await _db.PRN.CountAsync();
//                int nextNumber = 1;

//                if (prnCount > 0)
//                {
//                    var maxPrnNo = await _db.PRN
//                        .Where(p => p.ReturnNo.StartsWith("PRN-"))
//                        .Select(p => p.ReturnNo)
//                        .OrderByDescending(p => p)
//                        .FirstOrDefaultAsync();

//                    if (!string.IsNullOrEmpty(maxPrnNo))
//                    {
//                        var numberPart = maxPrnNo.Replace("PRN-", "");
//                        if (int.TryParse(numberPart, out int lastNumber))
//                        {
//                            nextNumber = lastNumber + 1;
//                        }
//                    }
//                    else
//                    {
//                        nextNumber = prnCount + 1;
//                    }
//                }

//                return $"PRN-{nextNumber:D9}";
//            }
//            catch (Exception ex)
//            {
//                return "PRN-000000001";
//            }
//        }

//        // Handler for medicine search
//        public async Task<JsonResult> OnGetSearchMedicines(string searchTerm)
//        {
//            var medicines = await _db.Medicines
//                .Where(m => m.Name.Contains(searchTerm) || m.Description.Contains(searchTerm))
//                .Where(m => m.IsActive)
//                .Select(m => new
//                {
//                    m.MedicineID,
//                    m.Name,
//                    m.Description
//                })
//                .Take(10)
//                .ToListAsync();

//            return new JsonResult(medicines);
//        }

//        // Handler for getting batches
//        public async Task<JsonResult> OnGetGetBatches(int medicineId)
//        {
//            var batches = await _db.MedicineBatches
//                .Where(b => b.MedicineID == medicineId && b.Quantity > 0)
//                .OrderBy(b => b.ExpiryDate)
//                .Select(b => new
//                {
//                    b.BatchID,
//                    b.BatchNumber,
//                    b.ManufactureDate,
//                    b.ExpiryDate,
//                    b.Quantity,
//                    b.PurchasePrice,
//                    DaysToExpiry = b.ExpiryDate.HasValue ?
//                        (b.ExpiryDate.Value - DateTime.Today).Days :
//                        (int?)null
//                })
//                .ToListAsync();

//            return new JsonResult(batches);
//        }

//        public class ItemDto
//        {
//            public int MedicineId { get; set; }
//            public string MedicineName { get; set; } = string.Empty;
//            public int BatchId { get; set; }
//            public string BatchNumber { get; set; } = string.Empty;
//            public int Qty { get; set; }
//            public decimal CostPrice { get; set; }
//            public decimal SubTotal { get; set; }
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            Debug.WriteLine("=== PRN SAVE STARTED ===");
//            Console.WriteLine("=== PRN SAVE STARTED ===");

//            // FIRST: Check if Supplier is selected
//            if (PRN.SupplierId == 0)
//            {
//                Debug.WriteLine("Supplier not selected - SupplierId is 0");
//                Console.WriteLine("Supplier not selected - SupplierId is 0");
//                ModelState.AddModelError("PRN.SupplierId", "Please select a supplier.");
//                await PopulateDropdowns();
//                return Page();
//            }

//            // SECOND: Check ModelState
//            if (!ModelState.IsValid)
//            {
//                Debug.WriteLine("ModelState is invalid");
//                Console.WriteLine("ModelState is invalid");

//                // Log all model errors
//                foreach (var key in ModelState.Keys)
//                {
//                    var state = ModelState[key];
//                    foreach (var error in state.Errors)
//                    {
//                        Debug.WriteLine($"Model Error - {key}: {error.ErrorMessage}");
//                        Console.WriteLine($"Model Error - {key}: {error.ErrorMessage}");
//                    }
//                }

//                await PopulateDropdowns();
//                return Page();
//            }

//            // THIRD: Check ItemsJson
//            if (string.IsNullOrEmpty(ItemsJson))
//            {
//                Debug.WriteLine("ItemsJson is empty");
//                Console.WriteLine("ItemsJson is empty");
//                ModelState.AddModelError("", "No items provided!");
//                await PopulateDropdowns();
//                return Page();
//            }

//            Debug.WriteLine($"ItemsJson: {ItemsJson}");
//            Console.WriteLine($"ItemsJson: {ItemsJson}");

//            List<ItemDto>? items = JsonConvert.DeserializeObject<List<ItemDto>>(ItemsJson);

//            if (items == null || items.Count == 0)
//            {
//                Debug.WriteLine("No items in the list");
//                Console.WriteLine("No items in the list");
//                ModelState.AddModelError("", "Please add items.");
//                await PopulateDropdowns();
//                return Page();
//            }

//            try
//            {
//                Debug.WriteLine("Starting database transaction...");
//                Console.WriteLine("Starting database transaction...");

//                Debug.WriteLine($"PRN Details - SupplierId: {PRN.SupplierId}, ReturnNo: {PRN.ReturnNo}, ReturnDate: {PRN.ReturnDate}");
//                Console.WriteLine($"PRN Details - SupplierId: {PRN.SupplierId}, ReturnNo: {PRN.ReturnNo}, ReturnDate: {PRN.ReturnDate}");

//                decimal totalAmount = items.Sum(i => i.SubTotal);
//                PRN.TotalAmount = totalAmount;

//                // Save PRN Header
//                Debug.WriteLine("Adding PRN to database...");
//                Console.WriteLine("Adding PRN to database...");
//                _db.PRN.Add(PRN);
//                await _db.SaveChangesAsync();

//                Debug.WriteLine($"PRN saved with ID: {PRN.Id}");
//                Console.WriteLine($"PRN saved with ID: {PRN.Id}");

//                // Save PRN Items and update batch quantities
//                foreach (var it in items)
//                {
//                    Debug.WriteLine($"Processing item - MedicineId: {it.MedicineId}, BatchId: {it.BatchId}, Qty: {it.Qty}");
//                    Console.WriteLine($"Processing item - MedicineId: {it.MedicineId}, BatchId: {it.BatchId}, Qty: {it.Qty}");

//                    // Save PRN Item
//                    PRNItem row = new PRNItem
//                    {
//                        PRNId = PRN.Id,
//                        MedicineId = it.MedicineId,
//                        BatchId = it.BatchId,
//                        Qty = it.Qty,
//                        CostPrice = it.CostPrice,
//                        SubTotal = it.SubTotal
//                    };

//                    _db.PRNItems.Add(row);

//                    // Update batch quantity
//                    var batch = await _db.MedicineBatches.FindAsync(it.BatchId);
//                    if (batch != null)
//                    {
//                        Debug.WriteLine($"Batch found - BatchID: {batch.BatchID}, Current Qty: {batch.Quantity}, Requested Qty: {it.Qty}");
//                        Console.WriteLine($"Batch found - BatchID: {batch.BatchID}, Current Qty: {batch.Quantity}, Requested Qty: {it.Qty}");

//                        if (batch.Quantity < it.Qty)
//                        {
//                            string errorMsg = $"Insufficient stock for batch {batch.BatchNumber}. Available: {batch.Quantity}, Requested: {it.Qty}";
//                            Debug.WriteLine($"ERROR: {errorMsg}");
//                            Console.WriteLine($"ERROR: {errorMsg}");
//                            throw new Exception(errorMsg);
//                        }

//                        batch.Quantity -= it.Qty;
//                        _db.MedicineBatches.Update(batch);
//                        Debug.WriteLine($"Batch quantity updated to: {batch.Quantity}");
//                        Console.WriteLine($"Batch quantity updated to: {batch.Quantity}");
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"Batch not found for BatchId: {it.BatchId}");
//                        Console.WriteLine($"Batch not found for BatchId: {it.BatchId}");
//                        throw new Exception($"Batch not found for BatchId: {it.BatchId}");
//                    }
//                }

//                Debug.WriteLine("Saving all changes to database...");
//                Console.WriteLine("Saving all changes to database...");
//                await _db.SaveChangesAsync();
//                Debug.WriteLine("All changes saved successfully!");
//                Console.WriteLine("All changes saved successfully!");

//                // Set success message
//                SuccessMessage = $"PRN {PRN.ReturnNo} Created Successfully!";
//                Debug.WriteLine($"PRN {PRN.ReturnNo} Created Successfully!");
//                Console.WriteLine($"PRN {PRN.ReturnNo} Created Successfully!");

//                // Redirect to same page with GET to refresh form
//                return RedirectToPage("/PRNcreate");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"ERROR: {ex.Message}");
//                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
//                Console.WriteLine($"ERROR: {ex.Message}");
//                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

//                ModelState.AddModelError("", $"Error saving PRN: {ex.Message}");
//                await PopulateDropdowns();
//                return Page();
//            }
//        }
//    }
//}



using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PHARMACY.Data;
using PHARMACY.Models;
using System;
using System.Diagnostics;

namespace PHARMACY.Pages
{
    public class PRNcreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PRNcreateModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public PRN PRN { get; set; } = new PRN();

        [BindProperty]
        public string ItemsJson { get; set; } = string.Empty;

        public IEnumerable<SelectListItem> SupplierList { get; set; } = new List<SelectListItem>();
        public List<Medicine> MedicineList { get; set; } = new();

        // Add property for success message
        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;

        public async Task OnGet()
        {
            await PopulateDropdowns();

            // Auto-generate return number
            PRN.ReturnNo = await GenerateReturnNumber();

            // Set default date to today
            PRN.ReturnDate = DateTime.Today;
        }

        private async Task PopulateDropdowns()
        {
            SupplierList = await _db.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            MedicineList = await _db.Medicines.ToListAsync();
        }

        private async Task<string> GenerateReturnNumber()
        {
            try
            {
                var prnCount = await _db.PRN.CountAsync();
                int nextNumber = 1;

                if (prnCount > 0)
                {
                    var maxPrnNo = await _db.PRN
                        .Where(p => p.ReturnNo.StartsWith("PRN-"))
                        .Select(p => p.ReturnNo)
                        .OrderByDescending(p => p)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(maxPrnNo))
                    {
                        var numberPart = maxPrnNo.Replace("PRN-", "");
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }
                    else
                    {
                        nextNumber = prnCount + 1;
                    }
                }

                return $"PRN-{nextNumber:D9}";
            }
            catch (Exception ex)
            {
                return "PRN-000000001";
            }
        }

        // Handler for medicine search
        public async Task<JsonResult> OnGetSearchMedicines(string searchTerm)
        {
            var medicines = await _db.Medicines
                .Where(m => m.Name.Contains(searchTerm) || m.Description.Contains(searchTerm))
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    m.MedicineID,
                    m.Name,
                    m.Description
                })
                .Take(10)
                .ToListAsync();

            return new JsonResult(medicines);
        }

        // Handler for getting batches
        public async Task<JsonResult> OnGetGetBatches(int medicineId)
        {
            var batches = await _db.MedicineBatches
                .Where(b => b.MedicineID == medicineId && b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.BatchID,
                    b.BatchNumber,
                    b.ManufactureDate,
                    b.ExpiryDate,
                    b.Quantity,
                    b.PurchasePrice,
                    DaysToExpiry = b.ExpiryDate.HasValue ?
                        (b.ExpiryDate.Value - DateTime.Today).Days :
                        (int?)null
                })
                .ToListAsync();

            return new JsonResult(batches);
        }

        public class ItemDto
        {
            public int MedicineId { get; set; }
            public string MedicineName { get; set; } = string.Empty;
            public int BatchId { get; set; }
            public string BatchNumber { get; set; } = string.Empty;
            public int Qty { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SubTotal { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Debug.WriteLine("=== PRN SAVE STARTED ===");
            Console.WriteLine("=== PRN SAVE STARTED ===");

            // FIRST: Check if Supplier is selected
            if (PRN.SupplierId == 0)
            {
                Debug.WriteLine("Supplier not selected - SupplierId is 0");
                Console.WriteLine("Supplier not selected - SupplierId is 0");
                ModelState.AddModelError("PRN.SupplierId", "Please select a supplier.");
                await PopulateDropdowns();
                return Page();
            }

            // SECOND: Check ModelState
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState is invalid");
                Console.WriteLine("ModelState is invalid");

                // Log all model errors
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Debug.WriteLine($"Model Error - {key}: {error.ErrorMessage}");
                        Console.WriteLine($"Model Error - {key}: {error.ErrorMessage}");
                    }
                }

                await PopulateDropdowns();
                return Page();
            }

            // THIRD: Check ItemsJson
            if (string.IsNullOrEmpty(ItemsJson))
            {
                Debug.WriteLine("ItemsJson is empty");
                Console.WriteLine("ItemsJson is empty");
                ModelState.AddModelError("", "No items provided!");
                await PopulateDropdowns();
                return Page();
            }

            Debug.WriteLine($"ItemsJson: {ItemsJson}");
            Console.WriteLine($"ItemsJson: {ItemsJson}");

            List<ItemDto>? items = JsonConvert.DeserializeObject<List<ItemDto>>(ItemsJson);

            if (items == null || items.Count == 0)
            {
                Debug.WriteLine("No items in the list");
                Console.WriteLine("No items in the list");
                ModelState.AddModelError("", "Please add items.");
                await PopulateDropdowns();
                return Page();
            }

            try
            {
                Debug.WriteLine("Starting database transaction...");
                Console.WriteLine("Starting database transaction...");

                Debug.WriteLine($"PRN Details - SupplierId: {PRN.SupplierId}, ReturnNo: {PRN.ReturnNo}, ReturnDate: {PRN.ReturnDate}");
                Console.WriteLine($"PRN Details - SupplierId: {PRN.SupplierId}, ReturnNo: {PRN.ReturnNo}, ReturnDate: {PRN.ReturnDate}");

                decimal totalAmount = items.Sum(i => i.SubTotal);
                PRN.TotalAmount = totalAmount;

                // Save PRN Header
                Debug.WriteLine("Adding PRN to database...");
                Console.WriteLine("Adding PRN to database...");
                _db.PRN.Add(PRN);
                await _db.SaveChangesAsync();

                Debug.WriteLine($"PRN saved with ID: {PRN.Id}");
                Console.WriteLine($"PRN saved with ID: {PRN.Id}");

                // Save PRN Items and update batch quantities
                foreach (var it in items)
                {
                    Debug.WriteLine($"Processing item - MedicineId: {it.MedicineId}, BatchId: {it.BatchId}, Qty: {it.Qty}");
                    Console.WriteLine($"Processing item - MedicineId: {it.MedicineId}, BatchId: {it.BatchId}, Qty: {it.Qty}");

                    // Save PRN Item
                    PRNItem row = new PRNItem
                    {
                        PRNId = PRN.Id,
                        MedicineId = it.MedicineId,
                        BatchId = it.BatchId,
                        Qty = it.Qty,
                        CostPrice = it.CostPrice,
                        SubTotal = it.SubTotal
                    };

                    _db.PRNItems.Add(row);

                    // Update batch quantity
                    var batch = await _db.MedicineBatches.FindAsync(it.BatchId);
                    if (batch != null)
                    {
                        Debug.WriteLine($"Batch found - BatchID: {batch.BatchID}, Current Qty: {batch.Quantity}, Requested Qty: {it.Qty}");
                        Console.WriteLine($"Batch found - BatchID: {batch.BatchID}, Current Qty: {batch.Quantity}, Requested Qty: {it.Qty}");

                        if (batch.Quantity < it.Qty)
                        {
                            string errorMsg = $"Insufficient stock for batch {batch.BatchNumber}. Available: {batch.Quantity}, Requested: {it.Qty}";
                            Debug.WriteLine($"ERROR: {errorMsg}");
                            Console.WriteLine($"ERROR: {errorMsg}");
                            throw new Exception(errorMsg);
                        }

                        batch.Quantity -= it.Qty;
                        _db.MedicineBatches.Update(batch);
                        Debug.WriteLine($"Batch quantity updated to: {batch.Quantity}");
                        Console.WriteLine($"Batch quantity updated to: {batch.Quantity}");
                    }
                    else
                    {
                        Debug.WriteLine($"Batch not found for BatchId: {it.BatchId}");
                        Console.WriteLine($"Batch not found for BatchId: {it.BatchId}");
                        throw new Exception($"Batch not found for BatchId: {it.BatchId}");
                    }
                }

                Debug.WriteLine("Saving all changes to database...");
                Console.WriteLine("Saving all changes to database...");
                await _db.SaveChangesAsync();
                Debug.WriteLine("All changes saved successfully!");
                Console.WriteLine("All changes saved successfully!");

                // Set success message
                SuccessMessage = $"PRN {PRN.ReturnNo} Created Successfully!";
                Debug.WriteLine($"PRN {PRN.ReturnNo} Created Successfully!");
                Console.WriteLine($"PRN {PRN.ReturnNo} Created Successfully!");

                // Redirect to same page with GET to refresh form
                return RedirectToPage("/PRNcreate");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", $"Error saving PRN: {ex.Message}");
                await PopulateDropdowns();
                return Page();
            }
        }
    }
}