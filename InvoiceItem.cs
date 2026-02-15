using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class InvoiceItem
    {
        [Key]
        public int InvoiceItemId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        [MaxLength(255)]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public int BatchId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        public DateTime MfgDate { get; set; }

        [Required]
        public DateTime ExpDate { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int FreeQty { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        // Navigation properties
        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }

        [ForeignKey("BatchId")]
        public virtual MedicineBatch? MedicineBatch { get; set; }
    }
}