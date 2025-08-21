using Domain;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers;
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // GET: /Admin
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _db.Appointments
            .Include(a => a.Service)
            .OrderBy(a => a.StartUtc)
            .Select(a => new AdminApptRow
            {
                Id = a.Id,
                Service = a.Service!.Name,
                Customer = a.CustomerName,
                Email = a.CustomerEmail,
                StartUtc = a.StartUtc,
                EndUtc = a.EndUtc,
                Status = a.Status.ToString(),
                DepositPaid = a.DepositPaid
            })
            .AsNoTracking()
            .ToListAsync();

        return View(items);
    }

    // POST: /Admin/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        if (appt.Status != AppointmentStatus.Cancelled)
        {
            appt.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Appointment cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    public record AdminApptRow
    {
        public Guid Id { get; init; }
        public string Service { get; init; } = "";
        public string Customer { get; init; } = "";
        public string Email { get; init; } = "";
        public DateTime StartUtc { get; init; }
        public DateTime EndUtc { get; init; }
        public string Status { get; init; } = "";
        public bool DepositPaid { get; init; }
    }
}
