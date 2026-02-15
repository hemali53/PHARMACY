using Microsoft.EntityFrameworkCore;
using PHARMACY.Models;
using PHARMACY.Pages.Inventory;

namespace PHARMACY.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<MedicineBatch> MedicineBatches { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<GRN> GRNs { get; set; } = null!;
        public DbSet<GRNItem> GRNItems { get; set; } = null!;
        public DbSet<PRN> PRN { get; set; }
        public DbSet<PRNItem> PRNItems { get; set; }
        public DbSet<LowStockItem> LowStockItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Representative> Representatives { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure LowStockItem as a keyless entity
            modelBuilder.Entity<LowStockItem>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });

            // Configure Medicine
            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.HasKey(m => m.MedicineID);
                entity.Property(m => m.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(m => m.Description)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(m => m.ImagePath)
                    .HasMaxLength(500)
                    .HasDefaultValue("");

                entity.Property(m => m.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(m => m.IsActive)
                    .HasDefaultValue(true);

                entity.Property(m => m.MinimumStockLevel)
                    .HasDefaultValue(10);

                entity.Property(m => m.AverageCost)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired()
                    .HasDefaultValue(0m);

                entity.Property(m => m.IsVat)
                    .HasDefaultValue(false);

                entity.HasMany(m => m.Batches)
                    .WithOne(b => b.Medicine)
                    .HasForeignKey(b => b.MedicineID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure MedicineBatch
            modelBuilder.Entity<MedicineBatch>(entity =>
            {
                entity.HasKey(mb => mb.BatchID);

                entity.Property(mb => mb.BatchNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(mb => mb.Quantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(mb => mb.PurchasePrice)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired()
                    .HasDefaultValue(0m);

                entity.Property(mb => mb.SellingPrice)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired()
                    .HasDefaultValue(0m);

                entity.Property(mb => mb.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(mb => mb.Medicine)
                    .WithMany(m => m.Batches)
                    .HasForeignKey(mb => mb.MedicineID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StockAdjustment
            modelBuilder.Entity<StockAdjustment>(entity =>
            {
                entity.HasKey(sa => sa.AdjustmentID);

                entity.Property(sa => sa.BatchNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(sa => sa.AdjustmentType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(sa => sa.Reason)
                    .HasMaxLength(500)
                    .HasDefaultValue("");

                entity.Property(sa => sa.AdjustmentDate)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(sa => sa.AdjustedBy)
                    .HasDefaultValue("System");

                entity.HasOne(sa => sa.Medicine)
                    .WithMany()
                    .HasForeignKey(sa => sa.MedicineID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure GRNItem
            modelBuilder.Entity<GRNItem>(entity =>
            {
                entity.HasKey(gi => gi.Id);

                entity.Property(gi => gi.ProductName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(gi => gi.Quantity)
                    .HasColumnType("decimal(18,3)");

                entity.Property(gi => gi.PurchasePrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(gi => gi.DiscountPercent)
                    .HasColumnType("decimal(5,2)");

                entity.Property(gi => gi.LineTotal)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(gi => gi.GRN)
                    .WithMany(g => g.Items)
                    .HasForeignKey(gi => gi.GRNId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PRN
            modelBuilder.Entity<PRN>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.ReturnNo)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.ReturnDate)
                    .IsRequired();

                entity.Property(p => p.TotalAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // Relationship with Supplier
                entity.HasOne(p => p.Supplier)
                    .WithMany()
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PRNItem
            modelBuilder.Entity<PRNItem>(entity =>
            {
                entity.HasKey(pi => pi.Id);

                entity.Property(pi => pi.Qty)
                    .IsRequired();

                entity.Property(pi => pi.CostPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(pi => pi.SubTotal)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // Relationship with PRN
                entity.HasOne(pi => pi.PRN)
                    .WithMany()
                    .HasForeignKey(pi => pi.PRNId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship with Medicine
                entity.HasOne(pi => pi.Medicine)
                    .WithMany()
                    .HasForeignKey(pi => pi.MedicineId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship with Batch
                entity.HasOne(pi => pi.Batch)
                    .WithMany()
                    .HasForeignKey(pi => pi.BatchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Representative
            modelBuilder.Entity<Representative>(entity =>
            {
                entity.HasKey(e => e.RepresentativeId);
                entity.Property(e => e.RepresentativeName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ContactNumber).HasMaxLength(15);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
            });
        }
    }
}