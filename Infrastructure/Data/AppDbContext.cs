using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Infrastructure.Identity;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Service>(e =>
        {
            e.ToTable("Services");
            e.HasKey(s => s.Id);

            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.DurationMinutes).IsRequired();

            // use cents (int) – no value converter needed
            e.Property(p => p.PriceCents).IsRequired();

            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.Description).HasMaxLength(2000);

            // If Service has a collection navigation:
            e.HasMany(s => s.Appointments)
             .WithOne(a => a.Service)
             .HasForeignKey(a => a.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Appointment>(e =>
        {
            e.ToTable("Appointments");
            e.HasKey(a => a.Id);

            e.Property(a => a.CustomerName).HasMaxLength(120).IsRequired();
            e.Property(a => a.CustomerEmail).HasMaxLength(256).IsRequired();
        });
    }
}
