using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;

namespace PHARMACY.Pages.Medicines
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Medicine> Medicines { get; set; } = new List<Medicine>();

        public async Task OnGetAsync()
        {
            try
            {
                // Safe query with explicit selection to handle NULLs
                Medicines = await _context.Medicines
                    .Select(m => new Medicine
                    {
                        MedicineID = m.MedicineID,
                        Name = m.Name ?? "Unknown",
                        Description = m.Description ?? "",
                        //Price = m.Price,
                        ManufactureDate = m.ManufactureDate,
                        ExpiryDate = m.ExpiryDate,
                        ImagePath = m.ImagePath ?? "",
                        CreatedDate = m.CreatedDate,
                        IsActive = m.IsActive
                    })
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Medicines = new List<Medicine>();

                // Add temporary data for testing
                Medicines.Add(new Medicine
                {
                    MedicineID = 1,
                    Name = "Test Medicine",
                    Description = "Test Description",
                    //Price = 100
                });
            }
        }
    }
}



