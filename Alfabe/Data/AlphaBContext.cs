using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Alfabe.Models;

namespace Alfabe.Data
{
    public class AlfabeContext : IdentityDbContext<AppUser>
    {
        public AlfabeContext(DbContextOptions<AlfabeContext> options) : base(options) { }

        public DbSet<Adres> Adresler { get; set; }
        public DbSet<DevamDurumu> DevamDurumlari { get; set; }
        public DbSet<Dersler> Dersler { get; set; }
        public DbSet<Ogrenci> Ogrenciler { get; set; }
        public DbSet<OgrenciDers> OgrenciDersler { get; set; }
        public DbSet<Odeme> Odemeler { get; set; }
        public DbSet<Ogretmen> Ogretmenler { get; set; }
        public DbSet<Rol> Roller { get; set; }
        public DbSet<Veli> Veliler { get; set; }
        public DbSet<Yoklama> Yoklamalar { get; set; }
        public DbSet<SmsLog> SmsLoglari { get; set; }

        public DbSet<VbsUser> VbsUsers { get; set; }

        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<GeneralExam> GeneralExams { get; set; }
        public DbSet<GeneralExamAnalysis> GeneralExamAnalyses { get; set; }
        public DbSet<GuidanceNote> GuidanceNotes { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<DerslerOgretmen> DerslerOgretmen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Veli>()
                .HasOne(v => v.Adres)
                .WithMany()
                .HasForeignKey(v => v.AdresId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Veli>()
                .HasOne(v => v.AppUser)
                .WithMany()
                .HasForeignKey(v => v.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ogrenci>()
                .HasOne(o => o.Veli)
                .WithMany(v => v.Ogrenciler)
                .HasForeignKey(o => o.VeliId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OgrenciDers>()
                .HasOne(od => od.Ogrenci)
                .WithMany(o => o.OgrenciDersler)
                .HasForeignKey(od => od.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OgrenciDers>()
                .HasOne(od => od.Ders)
                .WithMany(d => d.OgrenciDersler)
                .HasForeignKey(od => od.DersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Yoklama>()
                .HasOne(y => y.Ogrenci)
                .WithMany(o => o.Yoklamalar)
                .HasForeignKey(y => y.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Yoklama>()
                .HasOne(y => y.DevamDurumu)
                .WithMany(d => d.Yoklamalar)
                .HasForeignKey(y => y.DevamDurumuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Odeme>()
                .HasOne(o => o.Ogrenci)
                .WithMany(o => o.Odemeler)
                .HasForeignKey(o => o.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Odeme>()
                .Property(o => o.Tutar)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Homework>()
                .HasOne(h => h.Ders)
                .WithMany()
                .HasForeignKey(h => h.DersId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamResult>()
                .HasOne(e => e.Ogrenci)
                .WithMany()
                .HasForeignKey(e => e.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamResult>()
                .HasOne(e => e.Ders)
                .WithMany()
                .HasForeignKey(e => e.DersId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneralExam>()
                .HasOne(g => g.Ders)
                .WithMany()
                .HasForeignKey(g => g.DersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GeneralExamAnalysis>()
                .HasOne(a => a.GeneralExam)
                .WithMany(g => g.Analyses)
                .HasForeignKey(a => a.GeneralExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<DerslerOgretmen>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<DerslerOgretmen>()
                .HasOne(x => x.Ders)
                .WithMany(d => d.DerslerOgretmen)
                .HasForeignKey(x => x.DersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DerslerOgretmen>()
                .HasOne(x => x.Ogretmen)
                .WithMany(o => o.DerslerOgretmen)
                .HasForeignKey(x => x.OgretmenId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
