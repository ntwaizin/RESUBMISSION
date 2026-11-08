using Microsoft.EntityFrameworkCore;
using PROG_P2.Models;

namespace PROG_P2.Data
{
    /// <summary>
    /// EF Core database context. Defines the tables, keys, relationships and
    /// constraints so that the generated schema matches the Part 1 ERD / SQL script.
    /// </summary>
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
        {
        }

        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- Table names ----
            modelBuilder.Entity<Organiser>().ToTable("Organisers");
            modelBuilder.Entity<Participant>().ToTable("Participants");
            modelBuilder.Entity<Event>().ToTable("Events");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Enrolment>().ToTable("Enrolments");
            modelBuilder.Entity<Result>().ToTable("Results");
            modelBuilder.Entity<Account>().ToTable("Accounts");

            // ---- Column lengths / precision (match the ERD) ----
            modelBuilder.Entity<Organiser>(e =>
            {
                e.Property(o => o.FirstName).HasMaxLength(50);
                e.Property(o => o.LastName).HasMaxLength(50);
                e.Property(o => o.Email).HasMaxLength(100);
                e.Property(o => o.Phone).HasMaxLength(20);
                e.HasIndex(o => o.Email).IsUnique();
            });

            modelBuilder.Entity<Participant>(e =>
            {
                e.Property(p => p.FirstName).HasMaxLength(50);
                e.Property(p => p.LastName).HasMaxLength(50);
                e.Property(p => p.Email).HasMaxLength(100);
                e.Property(p => p.Phone).HasMaxLength(20);
                e.Property(p => p.DateOfBirth).HasColumnType("date");
                e.HasIndex(p => p.Email).IsUnique();
            });

            modelBuilder.Entity<Event>(e =>
            {
                e.Property(x => x.EventName).HasMaxLength(100);
                e.Property(x => x.Location).HasMaxLength(100);
                e.Property(x => x.Description).HasMaxLength(255);
                e.Property(x => x.EventType).HasMaxLength(10);
                e.Property(x => x.BannerUrl).HasMaxLength(500);
                e.Property(x => x.EventDate).HasColumnType("date");
                e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.Property(c => c.CategoryName).HasMaxLength(50);
                e.Property(c => c.DistanceKM).HasPrecision(5, 2);
                e.Property(c => c.EntryFee).HasPrecision(10, 2);
            });

            modelBuilder.Entity<Enrolment>(e =>
            {
                e.Property(x => x.PaymentStatus).HasMaxLength(20).HasDefaultValue("Pending");
                e.Property(x => x.EnrolmentDate).HasColumnType("date");
            });

            modelBuilder.Entity<Result>(e =>
            {
                e.Property(r => r.ResultStatus).HasMaxLength(20).HasDefaultValue("Pending");
                e.Property(r => r.FinishTime).HasColumnType("time");
            });

            modelBuilder.Entity<Account>(e =>
            {
                e.Property(a => a.Email).HasMaxLength(100);
                e.Property(a => a.PasswordHash).HasMaxLength(255);
                e.Property(a => a.Role).HasMaxLength(20);
                e.HasIndex(a => a.Email).IsUnique();
            });

            // ---- Relationships ----
            // Organiser 1 - * Event
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser).WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserID);

            // Event 1 - * Category
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event).WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID);

            // Participant 1 - * Enrolment
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant).WithMany(p => p.Enrolments)
                .HasForeignKey(e => e.ParticipantID);

            // Category 1 - * Enrolment
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryID);

            // Enrolment 1 - 0..1 Result
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment).WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID);

            // Account -> Organiser / Participant (one of the two is set)
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Organiser).WithMany()
                .HasForeignKey(a => a.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne(a => a.Participant).WithMany()
                .HasForeignKey(a => a.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
