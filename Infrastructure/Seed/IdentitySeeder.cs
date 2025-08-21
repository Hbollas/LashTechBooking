using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = config["Admin:Email"];
        var adminPassword = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return; // nothing to seed if not configured

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Ensure user exists
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var create = await userManager.CreateAsync(user, adminPassword);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new Exception($"Failed to create admin user: {msg}");
            }
        }

        // Ensure in role
        if (!await userManager.IsInRoleAsync(user, "Admin"))
            await userManager.AddToRoleAsync(user, "Admin");
    }
}
