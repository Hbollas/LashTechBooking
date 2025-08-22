using Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Desired catalog (prices in cents). Adjust durations if needed.
        // 1) Desired catalog (keep what you want to show) — add descriptions here:
        var desired = new[]
        {
            new Service { Name = "Classic",      DurationMinutes = 90, PriceCents = 5000, IsActive = true, Description = "Natural, clean and timeless." },
            new Service { Name = "Classic Fill", DurationMinutes = 60, PriceCents = 3000, IsActive = true, Description = "Refresh between full sets." },
            new Service { Name = "Wet Set",      DurationMinutes = 90, PriceCents = 5500, IsActive = true, Description = "Glossy, spiky, runway-inspired." },
            new Service { Name = "Wet Fill",     DurationMinutes = 60, PriceCents = 4000, IsActive = true, Description = "Maintain your Wet Set." },
            new Service { Name = "Hybrid",       DurationMinutes = 90, PriceCents = 6000, IsActive = true, Description = "Classic + Volume for soft drama." },
            new Service { Name = "Hybrid Fill",  DurationMinutes = 60, PriceCents = 4000, IsActive = true, Description = "Keep your Hybrid set fresh." },
            new Service { Name = "Volume",       DurationMinutes = 90, PriceCents = 7000, IsActive = true, Description = "Fuller, fluffy, camera-ready." },
            new Service { Name = "Volume Fill",  DurationMinutes = 60, PriceCents = 5000, IsActive = true, Description = "Top up your Volume set." },
            new Service { Name = "Mega Volume",  DurationMinutes = 90, PriceCents = 8000, IsActive = true, Description = "Maximum drama and density." },
            new Service { Name = "Mega Fill",    DurationMinutes = 60, PriceCents = 6000, IsActive = true, Description = "Maintain your Mega look." }
        };

        // Upsert by Name
        foreach (var s in desired)
        {
            var existing = await db.Services.SingleOrDefaultAsync(x => x.Name == s.Name);
            if (existing is null)
            {
                db.Services.Add(s);
            }
            else
            {
                existing.PriceCents      = s.PriceCents;
                existing.DurationMinutes = s.DurationMinutes;
                existing.IsActive        = s.IsActive;
                existing.Description     = s.Description;
            }
        }

        // Helper to normalize names: lower-case, unify dashes, remove spaces
        static string Norm(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var n = s.Trim().ToLowerInvariant()
                .Replace("\u2013", "-")   // en dash → hyphen
                .Replace("\u2014", "-");  // em dash → hyphen
            // remove ALL spaces
            return new string(n.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
        }

        // Names we want gone (normalized keys)
        var removeTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "classicfullset",
            "fill(2-3weeks)",   // handles "Fill (2-3 weeks)" and variants
            "lashremoval",
            "volumefullset"
        };

        // Deactivate (or delete) any matching service by normalized name
        var allServices = await db.Services.ToListAsync();
        foreach (var svc in allServices)
        {
            var key = Norm(svc.Name);

            // also handle en-dash version explicitly
            if (removeTargets.Contains(key) || key.Contains("fill(2-3weeks)"))
            {
                if (await db.Appointments.AnyAsync(a => a.ServiceId == svc.Id))
                {
                    svc.IsActive = false;   // keep history intact
                }
                else
                {
                    db.Services.Remove(svc);
                }
            }
        }

        await db.SaveChangesAsync();


    }
}