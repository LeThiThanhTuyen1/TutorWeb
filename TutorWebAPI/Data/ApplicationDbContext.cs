using Microsoft.EntityFrameworkCore;
using TutorWebAPI.Models;
using TutorWebAPI.Models.Entities;

namespace TutorWebAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Tutor>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                    .Property(c => c.Fee)
                    .HasPrecision(18, 2);

            modelBuilder.Entity<Contract>()
                .Property(c => c.Fee)
                .HasPrecision(18, 2);

            // Enrollment relationships
            modelBuilder.Entity<Enrollment>()
                 .HasOne(e => e.Student)
                 .WithMany(s => s.Enrollments)
                 .HasForeignKey(e => e.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Enrollment)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EnrollmentId);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;

            var modifiedCourses = ChangeTracker.Entries<Course>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var course in modifiedCourses)
            {
                if (course.StartDate == today && course.Status == "coming")
                {
                    course.Status = "ongoing";
                }

                if (course.EndDate < today && course.Status == "ongoing")
                {
                    course.Status = "completed";
                }

                if (course.Status == "completed" || course.Status == "canceled")
                {
                    var relatedSchedules = Schedules.Where(s => s.CourseId == course.Id);
                    await relatedSchedules.ForEachAsync(s => s.Status = course.Status);
                }
            }

            var modifiedContracts = ChangeTracker.Entries<Contract>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var contract in modifiedContracts)
            {
                if (contract.EndDate < today && contract.Status == "active")
                {
                    contract.Status = "completed";
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
