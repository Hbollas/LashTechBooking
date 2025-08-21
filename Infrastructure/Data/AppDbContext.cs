using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Infrastructure.Identity;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // IMPORTANT: let Identity configure its tables first
        base.OnModelCreating(b);

        // Store money as integer cents (works well with SQLite)
        var priceConverter = new ValueConverter<decimal, long>(
            v => (long)Math.Round(v * 100m, 0, MidpointRounding.AwayFromZero),
            v => v / 100m
        );

        b.Entity<Service>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Price)
                .HasConversion(priceConverter)
                .HasColumnType("INTEGER"); // cents
        });

        b.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.Service)
             .WithMany()
             .HasForeignKey(a => a.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(a => a.CustomerName).HasMaxLength(120).IsRequired();
            e.Property(a => a.CustomerEmail).HasMaxLength(256).IsRequired();
        });
    }
}
