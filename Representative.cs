using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Models
{
    public class Representative
    {
        [Key]
        public int RepresentativeId { get; set; }

        [Required]
        [StringLength(100)]
        public string RepresentativeName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [StringLength(15)]
        public string? ContactNumber { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}