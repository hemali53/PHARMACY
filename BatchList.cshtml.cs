using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;

namespace PHARMACY.Pages.Medicines
{
    public class BatchListModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BatchListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int MedicineID { get; set; }
        public string MedicineName { get; set; } = "Unknown Medicine";
        public List<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();

        public IActionResult OnGet(int id)
        {
            try
            {
                MedicineID = id;

                // Get medicine name
                var medicine = _context.Medicines.Find(id);
                if (medicine != null)
                {
                    MedicineName = medicine.Name;
                }

                // Get batches
                Batches = _context.MedicineBatches
                    .Where(b => b.MedicineID == id)
                    .ToList();

                return Page();
            }
            catch (Exception ex)
            {
                // If error, still show the page but with empty data
                return Page();
            }
        }
    }
}