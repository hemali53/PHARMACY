using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineID { get; set; }

        [Required(ErrorMessage = "Medicine name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(255, ErrorMessage = "Description cannot be longer than 255 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Average cost is required")]
        [Range(0.01, 100000.00, ErrorMessage = "Average cost must be between 0.01 and 100000.00")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Average Cost (LKR)")]
        public decimal? AverageCost { get; set; }

        [Display(Name = "Apply VAT")]
        public bool IsVat { get; set; } = false;

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateTime? ManufactureDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Image")]
        public string ImagePath { get; set; } = string.Empty;

        [Display(Name = "Minimum Stock Level")]
        [Range(0, 10000)]
        public int? MinimumStockLevel { get; set; } = 10;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property for batches
        public virtual ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();
    }
}