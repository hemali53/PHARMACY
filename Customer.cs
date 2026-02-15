//using System.ComponentModel.DataAnnotations;

//namespace PHARMACY.Models
//{
//    public class Customer
//    {
//        public int Id { get; set; }

//        [Required(ErrorMessage = "Customer name is required")]
//        [Display(Name = "Full Name")]
//        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
//        public string Name { get; set; } = string.Empty;

//        [Required(ErrorMessage = "Phone number is required")]
//        [Display(Name = "Phone Number")]
//        [RegularExpression(@"^(?:7|0|(?:\+94))[0-9]{9,10}$", ErrorMessage = "Please enter a valid phone number")]
//        [StringLength(15, ErrorMessage = "Phone number cannot be longer than 15 characters")]
//        public string PhoneNumber { get; set; } = string.Empty;

//        [Required(ErrorMessage = "Address is required")]
//        [Display(Name = "Address")]
//        [StringLength(255, ErrorMessage = "Address cannot be longer than 255 characters")]
//        public string Address { get; set; } = string.Empty;

//        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
//        [Display(Name = "Email Address")]
//        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
//        public string? Email { get; set; }

//        [Display(Name = "Area / Town")]
//        [Required(ErrorMessage = "Area or Town is required")]
//        [StringLength(100, ErrorMessage = "Area cannot be longer than 100 characters")]
//        public string Area { get; set; } = string.Empty;

//        [Display(Name = "Is VAT Registered")]
//        public bool IsVat { get; set; } = false;

//        [Display(Name = "Registration Date")]
//        public DateTime RegistrationDate { get; set; } = DateTime.Now;


//    }
//}


using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^(?:7|0|(?:\+94))[0-9]{9,10}$", ErrorMessage = "Please enter a valid phone number")]
        [StringLength(15, ErrorMessage = "Phone number cannot be longer than 15 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(255, ErrorMessage = "Address cannot be longer than 255 characters")]
        public string Address { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string? Email { get; set; }

        [Display(Name = "Area / Town")]
        [Required(ErrorMessage = "Area or Town is required")]
        [StringLength(100, ErrorMessage = "Area cannot be longer than 100 characters")]
        public string Area { get; set; } = string.Empty;

        [Display(Name = "Is VAT Registered")]
        public bool IsVat { get; set; } = false;

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}