//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using PHARMACY.Data;
//using PHARMACY.Models;

//namespace PHARMACY.Pages
//{
//    public class CustomerRegisterModel : PageModel
//    {
//        private readonly ApplicationDbContext _context;

//        public CustomerRegisterModel(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [BindProperty]
//        public Models.Customer Customer { get; set; } = new Models.Customer();
//        public string SuccessMessage { get; set; } = string.Empty;
//        public string ErrorMessage { get; set; } = string.Empty;

//        public void OnGet()
//        {
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            if (!ModelState.IsValid)
//            {
//                return Page();
//            }

//            try
//            {
//                // Set RegistrationDate to current date/time
//                Customer.RegistrationDate = DateTime.Now;

//                // Check if phone number already exists
//                var existingCustomer = _context.Customers
//                    .FirstOrDefault(c => c.PhoneNumber == Customer.PhoneNumber);

//                if (existingCustomer != null)
//                {
//                    ErrorMessage = $"Customer with phone number {Customer.PhoneNumber} already exists!";
//                    return Page();
//                }

//                _context.Customers.Add(Customer);
//                await _context.SaveChangesAsync();

//                SuccessMessage = $"Customer '{Customer.Name}' registered successfully! Customer ID: {Customer.Id}";

//                // Clear form after successful registration
//                Customer = new Models.Customer();
//                ModelState.Clear();

//                return Page();
//            }
//            catch (Exception ex)
//            {
//                ErrorMessage = $"Error registering customer: {ex.Message}";

//                // Log inner exception for more details
//                if (ex.InnerException != null)
//                {
//                    ErrorMessage += $" Inner Exception: {ex.InnerException.Message}";
//                }

//                return Page();
//            }
//        }
//    }
//}




using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PHARMACY.Data;

namespace PHARMACY.Pages
{
    public class CustomerRegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CustomerRegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PHARMACY.Models.Customer NewCustomer { get; set; } = new PHARMACY.Models.Customer();

        public List<PHARMACY.Models.Customer> CustomerList { get; set; } = new List<PHARMACY.Models.Customer>();
        public string SuccessMessage { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public async Task OnGetAsync()
        {
            await LoadCustomers();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCustomers();
                return Page();
            }

            try
            {
                NewCustomer.RegistrationDate = DateTime.Now;

                // Check if phone exists
                var exists = await _context.Customers
                    .AnyAsync(c => c.PhoneNumber == NewCustomer.PhoneNumber);

                if (exists)
                {
                    ErrorMessage = "Phone number already registered!";
                    await LoadCustomers();
                    return Page();
                }

                _context.Customers.Add(NewCustomer);
                await _context.SaveChangesAsync();

                SuccessMessage = $"Customer '{NewCustomer.Name}' registered! ID: {NewCustomer.Id}";

                // Clear form
                NewCustomer = new PHARMACY.Models.Customer();
                ModelState.Clear();

                await LoadCustomers();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                await LoadCustomers();
                return Page();
            }
        }

        private async Task LoadCustomers()
        {
            CustomerList = await _context.Customers
                .OrderByDescending(c => c.RegistrationDate)
                .Take(100)
                .ToListAsync();
        }
    }
}