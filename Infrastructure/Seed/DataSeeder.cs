using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Make sure DB is created & migrated
        await db.Database.MigrateAsync();

        if (!await db.Services.AnyAsync())
        {
            db.Services.AddRange(
                new Service { Name = "Classic Full Set", DurationMinutes = 90, Price = 89.00m, Description = "Natural, everyday look." },
                new Service { Name = "Volume Full Set", DurationMinutes = 120, Price = 129.00m, Description = "Fuller, dramatic look." },
                new Service { Name = "Fill (2–3 weeks)", DurationMinutes = 60, Price = 59.00m, Description = "Maintenance fill." },
                new Service { Name = "Lash Removal", DurationMinutes = 30, Price = 25.00m, Description = "Safe removal." }
            );
            await db.SaveChangesAsync();
        }
    }
}
