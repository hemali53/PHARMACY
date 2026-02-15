//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace PHARMACY.Models
//{
//    public class PRN
//    {
//        [Key]
//        public int Id { get; set; }

//        [Required(ErrorMessage = "Supplier is required")]
//        public int SupplierId { get; set; }

//        public string ReturnNo { get; set; } = string.Empty;

//        public DateTime ReturnDate { get; set; }

//        public decimal TotalAmount { get; set; }

//        [ForeignKey("SupplierId")]
//        public Supplier? Supplier { get; set; }

//        public int? InvoiceId { get; set; }

//        [ForeignKey("InvoiceId")]
//        public virtual Invoice? Invoice { get; set; }

//    }
//}



using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PHARMACY.Models
{
    public class PRN
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        public string ReturnNo { get; set; } = string.Empty;

        public DateTime ReturnDate { get; set; }

        public decimal TotalAmount { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
    }
}