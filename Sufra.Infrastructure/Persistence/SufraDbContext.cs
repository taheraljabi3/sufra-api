using Microsoft.EntityFrameworkCore;
using Sufra.Domain.Entities;

namespace Sufra.Infrastructure.Persistence
{
    public class SufraDbContext : DbContext
    {
        public SufraDbContext(DbContextOptions<SufraDbContext> options) : base(options) { }

        // 🗂️ الجداول
        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentHousing> StudentHousings => Set<StudentHousing>();
        public DbSet<Courier> Couriers => Set<Courier>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<MealRequest> MealRequests => Set<MealRequest>();
        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<BatchItem> BatchItems => Set<BatchItem>();
        public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<StudentHousing> StudentHousing { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 🧩 Subscription ↔ Student (1 - Many)
            // ============================================================
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Subscriptions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // 🧩 MealRequest ↔ Student (Many - 1)
            // ============================================================
            modelBuilder.Entity<MealRequest>()
                .HasOne(m => m.Student)
                .WithMany(st => st.MealRequests)
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 MealRequest ↔ Subscription (Many - 1)
            // ============================================================
            modelBuilder.Entity<MealRequest>()
                .HasOne(m => m.Subscription)
                .WithMany()
                .HasForeignKey(m => m.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 MealRequest ↔ Zone (Many - 1)
            // ============================================================
            modelBuilder.Entity<MealRequest>()
                .HasOne(m => m.Zone)
                .WithMany(z => z.MealRequests)
                .HasForeignKey(m => m.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 DeliveryProof ↔ MealRequest (1 - 1)
            // ============================================================
            modelBuilder.Entity<DeliveryProof>()
                .HasOne(dp => dp.MealRequest)
                .WithOne(mr => mr.DeliveryProof)
                .HasForeignKey<DeliveryProof>(dp => dp.MealRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // 🧩 DeliveryProof ↔ Courier (Many - 1)
            // ============================================================
            modelBuilder.Entity<DeliveryProof>()
                .HasOne(dp => dp.Courier)
                .WithMany(c => c.DeliveryProofs)
                .HasForeignKey(dp => dp.CourierId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 BatchItem (Many - Many) Batch ↔ MealRequest
            // ============================================================
            modelBuilder.Entity<BatchItem>()
                .HasKey(bi => new { bi.BatchId, bi.ReqId });

            modelBuilder.Entity<BatchItem>()
                .HasOne(bi => bi.Batch)
                .WithMany(b => b.Items)
                .HasForeignKey(bi => bi.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BatchItem>()
                .HasOne(bi => bi.MealRequest)
                .WithMany(mr => mr.BatchItems)
                .HasForeignKey(bi => bi.ReqId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 Batch ↔ Zone (Many - 1)
            // ============================================================
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Zone)
                .WithMany(z => z.Batches)
                .HasForeignKey(b => b.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 Batch ↔ Courier (Many - 1)
            // ============================================================
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Courier)
                .WithMany(c => c.Batches)
                .HasForeignKey(b => b.CourierId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // 🧩 Courier ↔ Student (Many - 1)
            // ============================================================
            modelBuilder.Entity<Courier>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 Courier ↔ Zone (Many - 1)
            // ============================================================
            modelBuilder.Entity<Courier>()
                .HasOne(c => c.Zone)
                .WithMany(z => z.Couriers)
                .HasForeignKey(c => c.ZoneId)
                .OnDelete(DeleteBehavior.NoAction);

            // ============================================================
            // 🧩 StudentHousing ↔ Student (Many - 1)
            // ============================================================
            modelBuilder.Entity<StudentHousing>()
                .HasOne(sh => sh.Student)
                .WithMany(st => st.Housings)
                .HasForeignKey(sh => sh.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // 🧩 StudentHousing ↔ Zone (Many - 1)
            // ============================================================
            modelBuilder.Entity<StudentHousing>()
                .HasOne(sh => sh.Zone)
                .WithMany(z => z.StudentHousings)
                .HasForeignKey(sh => sh.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================
            // ✅ تصغير أسماء الجداول والأعمدة لتتوافق مع PostgreSQL
            // ============================================================
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName()!.ToLowerInvariant());

                foreach (var property in entity.GetProperties())
                    property.SetColumnName(property.GetColumnBaseName().ToLowerInvariant());
            }
        }
    }
}
