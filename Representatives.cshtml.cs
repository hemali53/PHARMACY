using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Pages.Billing
{
    public class RepresentativesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RepresentativesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RepresentativeInputModel Representative { get; set; } = new RepresentativeInputModel();

        public List<PHARMACY.Models.Representative> Representatives { get; set; } = new List<PHARMACY.Models.Representative>();

        public async Task OnGetAsync()
        {
            await LoadRepresentativesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadRepresentativesAsync();
                return Page();
            }

            try
            {
                if (Representative.RepresentativeId == 0)
                {
                    // Add new representative
                    if (await CodeExistsAsync(Representative.Code))
                    {
                        ModelState.AddModelError("Representative.Code", "This code already exists.");
                        await LoadRepresentativesAsync();
                        return Page();
                    }

                    var newRep = new PHARMACY.Models.Representative
                    {
                        RepresentativeName = Representative.RepresentativeName,
                        Code = Representative.Code,
                        ContactNumber = Representative.ContactNumber,
                        Email = Representative.Email,
                        IsActive = Representative.IsActive,
                        CreatedDate = DateTime.Now
                    };

                    _context.Representatives.Add(newRep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Representative added successfully!";
                }
                else
                {
                    // Update existing representative
                    var existingRep = await _context.Representatives
                        .FirstOrDefaultAsync(r => r.RepresentativeId == Representative.RepresentativeId);

                    if (existingRep == null)
                    {
                        TempData["ErrorMessage"] = "Representative not found!";
                        await LoadRepresentativesAsync();
                        return Page();
                    }

                    // Check if code already exists for other representatives
                    if (await CodeExistsAsync(Representative.Code, Representative.RepresentativeId))
                    {
                        ModelState.AddModelError("Representative.Code", "This code already exists for another representative.");
                        await LoadRepresentativesAsync();
                        return Page();
                    }

                    existingRep.RepresentativeName = Representative.RepresentativeName;
                    existingRep.Code = Representative.Code;
                    existingRep.ContactNumber = Representative.ContactNumber;
                    existingRep.Email = Representative.Email;
                    existingRep.IsActive = Representative.IsActive;

                    _context.Representatives.Update(existingRep);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Representative updated successfully!";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving representative: {ex.Message}";
                await LoadRepresentativesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeactivateAsync(int id)
        {
            try
            {
                await UpdateRepresentativeStatusAsync(id, false);
                TempData["SuccessMessage"] = "Representative deactivated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deactivating representative: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            try
            {
                await UpdateRepresentativeStatusAsync(id, true);
                TempData["SuccessMessage"] = "Representative activated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error activating representative: {ex.Message}";
            }

            return RedirectToPage();
        }

        private async Task<bool> CodeExistsAsync(string code, int excludeId = 0)
        {
            return await _context.Representatives
                .AnyAsync(r => r.Code == code && r.RepresentativeId != excludeId);
        }

        private async Task LoadRepresentativesAsync()
        {
            try
            {
                Representatives = await _context.Representatives
                    .OrderByDescending(r => r.IsActive)
                    .ThenBy(r => r.RepresentativeName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading representatives: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading representatives: {ex.Message}";
                Representatives = new List<PHARMACY.Models.Representative>();
            }
        }

        private async Task UpdateRepresentativeStatusAsync(int id, bool isActive)
        {
            var representative = await _context.Representatives
                .FirstOrDefaultAsync(r => r.RepresentativeId == id);

            if (representative == null)
            {
                throw new Exception("Representative not found");
            }

            representative.IsActive = isActive;
            _context.Representatives.Update(representative);
            await _context.SaveChangesAsync();
        }
    }

    public class RepresentativeInputModel
    {
        public int RepresentativeId { get; set; }

        [Required(ErrorMessage = "Representative name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string RepresentativeName { get; set; } = "";

        [Required(ErrorMessage = "Code is required")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        public string Code { get; set; } = "";

        [StringLength(15, ErrorMessage = "Contact number cannot exceed 15 characters")]
        public string ContactNumber { get; set; } = "";

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}