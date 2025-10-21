using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ElCalculation> uvw_El_Calculation { get; set; }
        public DbSet<ElCalculationNotes> El_Calculation_Notes { get; set; }

        // เพิ่ม DbSet สำหรับ cross-db query
        public DbSet<Users> Users { get; set; }
        public DbSet<UserBranch> UserBranches { get; set; }
        public DbSet<RptElCalculateSale> Rpt_EL_CalculateSale { get; set; }
        public DbSet<RptELProductionTime> rpt_El_ProductionTime { get; set; }
        public DbSet<RptELSeatingLost> Rpt_EL_SeatingLost { get; set; }

        public DbSet<EL_LossSummary> EL_LossSummary { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ElCalculation>().HasKey(c => c.RunId);

            modelBuilder.Entity<ElCalculationNotes>().HasKey(n => n.NoteId);

            modelBuilder
                .Entity<ElCalculationNotes>()
                .HasOne(n => n.Calculation)
                .WithMany()
                .HasForeignKey(n => n.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Keyless Entities สำหรับ read-only cross-db query
            modelBuilder.Entity<Users>().HasNoKey();
            modelBuilder.Entity<RptELProductionTime>().HasNoKey();
            modelBuilder.Entity<RptELSeatingLost>().HasNoKey();
            modelBuilder.Entity<UserBranch>().HasNoKey();
            modelBuilder
                .Entity<RptElCalculateSale>()
                .HasKey(e => new { e.Branch_Code, e.OrderDate });
        }
    }
}
