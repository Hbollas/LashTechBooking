using Infrastructure.Data;
using Infrastructure.Seed;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=../App_Data/lashtech.db"; // local dev path

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));


builder.Services.AddDefaultIdentity<Infrastructure.Identity.ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 10;
})
.AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
.AddEntityFrameworkStores<Infrastructure.Data.AppDbContext>();

builder.Services.AddRazorPages();
var app = builder.Build();
// Seed dev data at startup
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(db);

    // NEW: seed admin role/user
    await Infrastructure.Seed.IdentitySeeder.SeedAdminAsync(sp, builder.Configuration);
}

// Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); 
app.Run();
