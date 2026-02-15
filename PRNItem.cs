//using System.ComponentModel.DataAnnotations;

//namespace PHARMACY.Models
//{
//    public class PRNItem
//    {
//        [Key]
//        public int Id { get; set; }
//        public int PRNId { get; set; }
//        public int MedicineId { get; set; }
//        public int BatchId { get; set; } 
//        public int Qty { get; set; }
//        public decimal CostPrice { get; set; }
//        public decimal SubTotal { get; set; }

//        public PRN PRN { get; set; }
//        public Medicine Medicine { get; set; }
//        public MedicineBatch Batch { get; set; } 
//    }
//}



using System.ComponentModel.DataAnnotations;

namespace PHARMACY.Models
{
    public class PRNItem
    {
        [Key]
        public int Id { get; set; }
        public int PRNId { get; set; }
        public int MedicineId { get; set; }
        public int BatchId { get; set; }
        public int Qty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SubTotal { get; set; }

        public PRN PRN { get; set; }
        public Medicine Medicine { get; set; }
        public MedicineBatch Batch { get; set; }
    }
}