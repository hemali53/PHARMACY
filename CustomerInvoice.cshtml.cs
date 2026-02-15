using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace PHARMACY.Pages.Billing
{
    public class CustomerInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerInvoiceModel> _logger;

        public CustomerInvoiceModel(ApplicationDbContext context, ILogger<CustomerInvoiceModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InvoiceModel Invoice { get; set; } = new InvoiceModel();

        [BindProperty]
        public List<InvoiceItemModel> InvoiceItems { get; set; } = new List<InvoiceItemModel>();

        public List<PHARMACY.Models.Customer> Customers { get; set; } = new List<PHARMACY.Models.Customer>();
        public List<PHARMACY.Models.Representative> Representatives { get; set; } = new List<PHARMACY.Models.Representative>();

        public async Task OnGetAsync()
        {
            try
            {
                await LoadCustomersAsync();
                await LoadRepresentativesAsync();

                // Set default date to today
                Invoice.InvoiceDate = DateTime.Now;

                // Only generate new invoice number if we're creating a new invoice
                if (string.IsNullOrEmpty(Invoice.InvoiceNumber))
                {
                    Invoice.InvoiceNumber = await GetNextInvoiceNumberAsync();
                    _logger.LogInformation($"Generated new invoice number: {Invoice.InvoiceNumber}");
                }

                _logger.LogInformation($"OnGetAsync completed. Invoice items count: {InvoiceItems.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync");
                TempData["ErrorMessage"] = "Error loading page: " + ex.Message;
            }
        }

        public async Task<JsonResult> OnGetSearchMedicinesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                {
                    return new JsonResult(new List<object>());
                }

                var medicines = await _context.Medicines
                    .Where(m => m.Name.Contains(searchTerm) ||
                           (m.Description != null && m.Description.Contains(searchTerm)))
                    .Select(m => new
                    {
                        MedicineId = m.MedicineID,
                        Name = m.Name,
                        Description = m.Description ?? "",
                        Code = m.MedicineID.ToString()
                    })
                    .Take(10)
                    .ToListAsync();

                return new JsonResult(medicines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching medicines");
                return new JsonResult(new List<object>());
            }
        }

        public async Task<JsonResult> OnGetGetBatchesByMedicineAsync(int medicineId)
        {
            try
            {
                var batches = await _context.MedicineBatches
                    .Where(b => b.MedicineID == medicineId && b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new
                    {
                        BatchId = b.BatchID,
                        BatchNumber = b.BatchNumber,
                        StockQuantity = b.Quantity,
                        UnitPrice = b.SellingPrice,
                        MfgDate = b.ManufactureDate.HasValue ? b.ManufactureDate.Value.ToString("yyyy-MM-dd") : "",
                        ExpDate = b.ExpiryDate.HasValue ? b.ExpiryDate.Value.ToString("yyyy-MM-dd") : ""
                    })
                    .ToListAsync();

                return new JsonResult(batches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading batches");
                return new JsonResult(new List<object>());
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveInvoiceAsync()
        {
            try
            {
                _logger.LogInformation("=== STARTING INVOICE SAVE ===");
                _logger.LogInformation($"ModelState IsValid: {ModelState.IsValid}");

                // Log ModelState errors if any
                if (!ModelState.IsValid)
                {
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        if (errors.Count > 0)
                        {
                            _logger.LogError($"ModelState error for {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                }

                // Log invoice data
                _logger.LogInformation($"Invoice Number: {Invoice.InvoiceNumber}");
                _logger.LogInformation($"Customer Name: {Invoice.CustomerName}");
                _logger.LogInformation($"Invoice Items Count from Model Binding: {InvoiceItems?.Count ?? 0}");

                // Validate that we have items
                if (InvoiceItems == null || !InvoiceItems.Any())
                {
                    _logger.LogWarning("No invoice items found in model binding");
                    ModelState.AddModelError("", "Please add at least one item to the invoice");
                    await LoadCustomersAsync();
                    await LoadRepresentativesAsync();
                    return Page();
                }

                // Log each item
                foreach (var item in InvoiceItems)
                {
                    _logger.LogInformation($"Item: {item.MedicineName}, Qty: {item.Quantity}, Price: {item.UnitPrice}");
                }

                // Generate invoice number if not set
                if (string.IsNullOrEmpty(Invoice.InvoiceNumber))
                {
                    Invoice.InvoiceNumber = await GetNextInvoiceNumberAsync();
                    _logger.LogInformation($"Generated invoice number: {Invoice.InvoiceNumber}");
                }

                // Handle customer
                int customerId = await HandleCustomerAsync();
                _logger.LogInformation($"Customer ID after handling: {customerId}");

                // Create invoice entity
                var invoice = new PHARMACY.Models.Invoice
                {
                    InvoiceNumber = Invoice.InvoiceNumber,
                    InvoiceDate = Invoice.InvoiceDate,
                    InvoiceType = Invoice.InvoiceType,
                    CustomerId = customerId > 0 ? customerId : (int?)null,
                    CustomerName = Invoice.CustomerName ?? string.Empty,
                    CustomerAddress = Invoice.CustomerAddress ?? string.Empty,
                    CustomerPhone = Invoice.CustomerPhone ?? string.Empty,
                    PONumber = Invoice.PONumber ?? string.Empty,
                    RepresentativeId = Invoice.RepresentativeId > 0 ? Invoice.RepresentativeId : (int?)null,
                    RepresentativeName = Invoice.RepresentativeName ?? string.Empty,
                    GrossValue = Invoice.GrossValue,
                    VATValue = Invoice.VATValue,
                    VATPercentage = Invoice.VATPercentage,
                    DiscountValue = Invoice.DiscountValue,
                    AddValue = Invoice.AddValue,
                    LessValue = Invoice.LessValue,
                    NetValue = Invoice.NetValue,
                    Note = Invoice.Note ?? string.Empty,
                    CreatedDate = DateTime.Now
                };

                _logger.LogInformation($"Created invoice entity. Gross: {invoice.GrossValue}, Net: {invoice.NetValue}");

                // Use transaction for data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Save invoice
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Invoice saved successfully with ID: {invoice.InvoiceId}");

                    // Save invoice items and update stock
                    await SaveInvoiceItemsAsync(invoice.InvoiceId);

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully");

                    // Set success message and store invoice details for printing
                    TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} saved successfully!";
                    TempData["SavedInvoiceId"] = invoice.InvoiceId;
                    TempData["SavedInvoiceNumber"] = invoice.InvoiceNumber;

                    _logger.LogInformation($"=== INVOICE SAVE COMPLETED SUCCESSFULLY ===");

                    // Redirect to clear form and show success
                    return RedirectToPage(new { id = invoice.InvoiceId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction rolled back");
                    throw;
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database error");
                TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error");
                TempData["ErrorMessage"] = $"Save failed: {dbEx.InnerException?.Message ?? dbEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error saving invoice");
                TempData["ErrorMessage"] = $"Error saving invoice: {ex.Message}";
            }

            await LoadCustomersAsync();
            await LoadRepresentativesAsync();
            return Page();
        }

        private async Task<int> HandleCustomerAsync()
        {
            _logger.LogInformation($"Handling customer. CustomerId: {Invoice.CustomerId}, CustomerName: {Invoice.CustomerName}");

            if (Invoice.CustomerId > 0)
            {
                // Check if customer exists
                var existingCustomer = await _context.Customers.FindAsync(Invoice.CustomerId);
                if (existingCustomer != null)
                {
                    _logger.LogInformation($"Found existing customer: {existingCustomer.Name}");

                    // Update customer details if provided
                    if (!string.IsNullOrEmpty(Invoice.CustomerAddress))
                        existingCustomer.Address = Invoice.CustomerAddress;
                    if (!string.IsNullOrEmpty(Invoice.CustomerPhone))
                        existingCustomer.PhoneNumber = Invoice.CustomerPhone;

                    await _context.SaveChangesAsync();
                    return existingCustomer.Id;
                }
                else
                {
                    _logger.LogWarning($"Customer with ID {Invoice.CustomerId} not found. Will create new.");
                }
            }

            // Create new customer
            if (!string.IsNullOrEmpty(Invoice.CustomerName))
            {
                _logger.LogInformation($"Creating new customer: {Invoice.CustomerName}");

                var newCustomer = new PHARMACY.Models.Customer
                {
                    Name = Invoice.CustomerName,
                    Address = Invoice.CustomerAddress ?? string.Empty,
                    PhoneNumber = Invoice.CustomerPhone ?? string.Empty,
                    Area = Invoice.CustomerAddress ?? "General",
                    Email = "",
                    IsVat = false,
                    RegistrationDate = DateTime.Now
                };

                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New customer created with ID: {newCustomer.Id}");
                return newCustomer.Id;
            }

            _logger.LogWarning("No customer name provided. Returning 0.");
            return 0;
        }

        private async Task SaveInvoiceItemsAsync(int invoiceId)
        {
            _logger.LogInformation($"Saving {InvoiceItems.Count} items for invoice ID: {invoiceId}");

            foreach (var item in InvoiceItems)
            {
                _logger.LogInformation($"Processing item: {item.MedicineName}, Batch: {item.BatchId}");

                // Update batch stock
                var batch = await _context.MedicineBatches.FindAsync(item.BatchId);
                if (batch == null)
                {
                    _logger.LogError($"Batch {item.BatchId} not found");
                    throw new Exception($"Batch {item.BatchId} not found");
                }

                _logger.LogInformation($"Batch found: {batch.BatchNumber}, Stock: {batch.Quantity}, Requested: {item.Quantity}");

                if (batch.Quantity < item.Quantity)
                {
                    _logger.LogError($"Insufficient stock for {item.MedicineName}. Available: {batch.Quantity}, Requested: {item.Quantity}");
                    throw new Exception($"Insufficient stock for {item.MedicineName}. Available: {batch.Quantity}, Requested: {item.Quantity}");
                }

                batch.Quantity -= item.Quantity;
                _context.MedicineBatches.Update(batch);
                _logger.LogInformation($"Batch stock updated. New stock: {batch.Quantity}");

                // Create invoice item
                var invoiceItem = new PHARMACY.Models.InvoiceItem
                {
                    InvoiceId = invoiceId,
                    MedicineId = item.MedicineId,
                    MedicineName = item.MedicineName,
                    Code = item.Code,
                    BatchId = item.BatchId,
                    BatchNumber = item.BatchNumber,
                    MfgDate = item.MfgDate,
                    ExpDate = item.ExpDate,
                    Quantity = item.Quantity,
                    FreeQty = item.FreeQty,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Amount,
                    DiscountPercent = item.DiscountPercent,
                    DiscountValue = item.DiscountValue,
                    NetAmount = item.NetAmount
                };

                _context.InvoiceItems.Add(invoiceItem);
                _logger.LogInformation($"Invoice item added: {item.MedicineName}");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("All invoice items saved successfully");
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            _logger.LogInformation("Clearing form");
            return RedirectToPage();
        }

        private async Task<string> GetNextInvoiceNumberAsync()
        {
            try
            {
                var lastInvoice = await _context.Invoices
                    .OrderByDescending(i => i.InvoiceId)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;

                if (lastInvoice != null)
                {
                    _logger.LogInformation($"Last invoice: {lastInvoice.InvoiceNumber}, ID: {lastInvoice.InvoiceId}");

                    // Try to extract number from existing invoice number
                    if (lastInvoice.InvoiceNumber.StartsWith("INV-"))
                    {
                        var numberPart = lastInvoice.InvoiceNumber.Substring(4);
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                            _logger.LogInformation($"Extracted number: {lastNumber}, Next: {nextNumber}");
                        }
                        else
                        {
                            nextNumber = lastInvoice.InvoiceId + 1;
                            _logger.LogInformation($"Could not parse number. Using InvoiceId + 1: {nextNumber}");
                        }
                    }
                    else
                    {
                        nextNumber = lastInvoice.InvoiceId + 1;
                        _logger.LogInformation($"Invoice number doesn't start with INV-. Using InvoiceId + 1: {nextNumber}");
                    }
                }
                else
                {
                    _logger.LogInformation("No invoices found. Starting from 1");
                }

                var invoiceNumber = $"INV-{nextNumber:D5}";
                _logger.LogInformation($"Generated invoice number: {invoiceNumber}");
                return invoiceNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                return $"INV-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        private async Task LoadCustomersAsync()
        {
            Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();
            _logger.LogInformation($"Loaded {Customers.Count} customers");
        }

        private async Task LoadRepresentativesAsync()
        {
            Representatives = await _context.Representatives
                .OrderBy(r => r.RepresentativeName)
                .ToListAsync();
            _logger.LogInformation($"Loaded {Representatives.Count} representatives");
        }
    }

    // View Models (same as before)
    public class InvoiceModel
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string InvoiceType { get; set; } = "Credit Invoice";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
        public int RepresentativeId { get; set; }
        public string RepresentativeName { get; set; } = string.Empty;
        public decimal GrossValue { get; set; }
        public decimal VATValue { get; set; }
        public decimal VATPercentage { get; set; } = 18;
        public decimal DiscountValue { get; set; }
        public decimal AddValue { get; set; }
        public decimal LessValue { get; set; }
        public decimal NetValue { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class InvoiceItemModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime MfgDate { get; set; }
        public DateTime ExpDate { get; set; }
        public int Quantity { get; set; } = 1;
        public int FreeQty { get; set; } = 0;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal NetAmount { get; set; }
    }
}