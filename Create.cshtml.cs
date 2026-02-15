using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;
using PHARMACY.Models;
using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Pages.Medicines
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Medicine Medicine { get; set; }

        [BindProperty]
        [Display(Name = "Medicine Image")]
        public IFormFile ImageFile { get; set; }

        public IActionResult OnGet()
        {
            Console.WriteLine("=== GET REQUEST ===");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("=== FORM SUBMITTED ===");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            // Debug: Print all model state values
            foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key];
                Console.WriteLine($"Key: {key}, IsValid: {value.ValidationState}");
                foreach (var error in value.Errors)
                {
                    Console.WriteLine($"  Error: {error.ErrorMessage}");
                }
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== MODEL STATE INVALID ===");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return Page();
            }

            try
            {
                Console.WriteLine("=== STARTING MEDICINE SAVE ===");
                Console.WriteLine($"Medicine Name: {Medicine?.Name}");
                Console.WriteLine($"Medicine AverageCost: {Medicine?.AverageCost}");
                Console.WriteLine($"VAT Applied: {Medicine?.IsVat}");
                Console.WriteLine($"Medicine Description: {Medicine?.Description}");
                Console.WriteLine($"ImageFile: {ImageFile?.FileName} (Size: {ImageFile?.Length} bytes)");

                // Handle image upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    Console.WriteLine("Processing image upload...");

                    // Validate image size (max 5MB)
                    if (ImageFile.Length > 5 * 1024 * 1024)
                    {
                        Console.WriteLine("Image too large");
                        ModelState.AddModelError("ImageFile", "Image size cannot exceed 5MB.");
                        return Page();
                    }

                    // Validate image type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        Console.WriteLine("Invalid image type");
                        ModelState.AddModelError("ImageFile", "Only image files (JPG, PNG, GIF) are allowed.");
                        return Page();
                    }

                    var fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName);
                    var newFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
                    var imagesFolder = Path.Combine(_environment.WebRootPath, "images", "medicines");
                    var filePath = Path.Combine(imagesFolder, newFileName);

                    Console.WriteLine($"Image Path: {filePath}");

                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(imagesFolder);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    Medicine.ImagePath = $"/images/medicines/{newFileName}";
                    Console.WriteLine($"Image saved to: {Medicine.ImagePath}");
                }
                else
                {
                    Console.WriteLine("No image uploaded");
                }

                // Set created date
                Medicine.CreatedDate = DateTime.Now;
                Medicine.IsActive = true;

                Console.WriteLine("Adding medicine to database context...");
                _context.Medicines.Add(Medicine);

                Console.WriteLine("Saving to database...");
                var result = await _context.SaveChangesAsync();
                Console.WriteLine($"Database save completed. Rows affected: {result}");

                Console.WriteLine($"Medicine ID after save: {Medicine.MedicineID}");

                TempData["SuccessMessage"] = $"Medicine '{Medicine.Name}' added successfully!";
                Console.WriteLine("Redirecting to Index page...");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"=== DATABASE UPDATE EXCEPTION ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An error occurred while saving the medicine. Please try again.");
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== GENERAL EXCEPTION ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return Page();
            }
        }
    }
}



