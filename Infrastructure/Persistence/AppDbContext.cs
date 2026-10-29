namespace Infrastructure.Persistence;

using Application.Abstractions.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<AdmissionCourse> AdmissionCourses => Set<AdmissionCourse>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Description)
                .HasMaxLength(500);

            entity.Property(p => p.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(p => p.StockQuantity)
                .IsRequired();
        });
    
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Email).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Admission>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AcademicYear).IsRequired().HasMaxLength(20);
            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId);
        });

        modelBuilder.Entity<AdmissionCourse>(entity =>
        {
            entity.HasKey(ac => ac.Id);
            entity.HasOne(ac => ac.Admission)
                  .WithMany(a => a.AdmissionCourses)
                  .HasForeignKey(ac => ac.AdmissionId);
            entity.HasOne(ac => ac.Course)
                  .WithMany()
                  .HasForeignKey(ac => ac.CourseId);
        });
    }
}
