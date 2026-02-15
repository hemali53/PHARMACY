using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;

namespace PHARMACY.Pages.Billing
{
    public class PrintInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PrintInvoiceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Invoice? Invoice { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Load invoice with items
                Invoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (Invoice == null)
                {
                    return NotFound();
                }

                InvoiceItems = Invoice.InvoiceItems.ToList();
                return Page();
            }
            catch (Exception ex)
            {
                // Log error
                return NotFound();
            }
        }
    }
}