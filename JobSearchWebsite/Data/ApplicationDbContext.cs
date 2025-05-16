using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JobSearchWebsite.Models;
using Microsoft.AspNetCore.Identity;

namespace JobSearchWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CareerGuide> CareerGuides { get; set; }
        public DbSet<JobSaved> JobSaveds { get; set; }
        public DbSet<ResumeTemplate> ResumeTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<JobApplication>()
                .HasOne(ja => ja.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "JobSeeker", NormalizedName = "JOBSEEKER" },
                new IdentityRole { Id = "2", Name = "Employer", NormalizedName = "EMPLOYER" },
                new IdentityRole { Id = "3", Name = "Admin", NormalizedName = "ADMIN" }
            );

            builder.Entity<Job>()
                .HasOne(j => j.User)
                .WithMany(u => u.Jobs)
                .HasForeignKey(j => j.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(p => p.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Job>()
                .Property(j => j.UserId)
                .HasColumnType("nvarchar(450)");

            builder.Entity<CareerGuide>()
                .HasOne(cg => cg.Author)
                .WithMany()
                .HasForeignKey(cg => cg.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobSaved>()
                .HasOne(js => js.Job)
                .WithMany()
                .HasForeignKey(js => js.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<JobSaved>()
                .HasOne(js => js.JobSeeker)
                .WithMany()
                .HasForeignKey(js => js.JobSeekerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ cho Notification
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
         
            builder.Entity<ResumeTemplate>()
                .HasOne(rt => rt.CreatedByUser)
                .WithMany()
                .HasForeignKey(rt => rt.CreatedBy)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}