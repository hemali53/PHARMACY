using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PHARMACY.Data;

namespace PHARMACY.Pages
{
    public class SupplierCreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public SupplierCreateModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Supplier Supplier { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            _db.Suppliers.Add(Supplier);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Supplier added successfully!";
            return RedirectToPage("/SupplierCreate");
        }
    }
}
