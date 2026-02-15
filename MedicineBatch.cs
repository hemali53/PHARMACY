using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class MedicineBatch
    {
        [Key]
        public int BatchID { get; set; }

        [Required]
        public int MedicineID { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateTime? ManufactureDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PurchasePrice { get; set; }

        [Required]
        [Range(0.01, 100000.00)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("MedicineID")]
        public virtual Medicine Medicine { get; set; }
    }
}