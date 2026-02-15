using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceType { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength]
        public string? CustomerAddress { get; set; }

        [MaxLength(50)]
        public string? CustomerPhone { get; set; }

        [MaxLength(100)]
        public string? PONumber { get; set; }

        public int? RepresentativeId { get; set; }

        [MaxLength(100)]
        public string? RepresentativeName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VATValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal VATPercentage { get; set; } = 18;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AddValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LessValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetValue { get; set; }

        [MaxLength]
        public string? Note { get; set; }

        public DateTime? CreatedDate { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}