using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class StockAdjustment
    {
        [Key]
        public int AdjustmentID { get; set; }

        [Required]
        public int MedicineID { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AdjustmentType { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "Adjustment Date")]
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;

        public string AdjustedBy { get; set; } = "System";

        // Navigation property
        [ForeignKey("MedicineID")]
        public virtual Medicine Medicine { get; set; }
    }
}