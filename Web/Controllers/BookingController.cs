using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Domain; 
using Web.Models;

namespace Web.Controllers;

public class BookingController : Controller
{
    private readonly AppDbContext _db;
    public BookingController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Create(Guid serviceId, DateOnly? date, string? selectedTime = null)
    {
        var svc = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);

        if (svc is null) return NotFound();

        var theDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var vm = new CreateBookingVm
        {
            ServiceId = svc.Id,
            ServiceName = svc.Name,
            ServiceDurationMinutes = svc.DurationMinutes,
            Date = theDate,
            TimeOptions = await GenerateSlotsAsync(svc.Id, theDate, svc.DurationMinutes)
        };

        ViewBag.SelectedTime = selectedTime ?? "";
        return View(vm);
    }


// very simple slot generator: every 30 minutes between 10:00–18:00 local time
private async Task<List<string>> GenerateSlotsAsync(Guid serviceId, DateOnly date, int durationMinutes)
{
    var open  = new TimeOnly(9, 0);
    var close = new TimeOnly(18, 0);
    var step  = TimeSpan.FromMinutes(30);

    var today = DateOnly.FromDateTime(DateTime.Today);
    var nowLocal = DateTime.Now;

    // No slots for past dates
    if (date < today) return new List<string>();

    // Baseline start at opening
    var startSpan = open.ToTimeSpan();

    // If booking for today, jump to the next 30-minute boundary after "now"
    if (date == today)
    {
        var next = nowLocal.AddMinutes(5); // small buffer
        var roundedMinutes = ((next.Minute / 30) * 30);
        if (roundedMinutes < next.Minute) roundedMinutes += 30;
        var rounded = new TimeOnly(next.Hour, roundedMinutes % 60);
        var roundedSpan = rounded.ToTimeSpan() + TimeSpan.FromHours(roundedMinutes >= 60 ? 1 : 0);

        if (roundedSpan > startSpan) startSpan = roundedSpan;
    }

    // Existing conflict logic
    var dayStartLocal = date.ToDateTime(open);
    var dayEndLocal   = date.ToDateTime(close);
    var dayStartUtc   = dayStartLocal.ToUniversalTime();
    var dayEndUtc     = dayEndLocal.ToUniversalTime();

   var taken = await _db.Appointments
        .Where(a => a.Status != Domain.AppointmentStatus.Cancelled)
        .Where(a => a.StartUtc < dayEndUtc && a.EndUtc > dayStartUtc)   // any overlap that day
        .Select(a => new { a.StartUtc, a.EndUtc })
        .ToListAsync();


    var slots = new List<string>();
    for (var t = startSpan; t <= close.ToTimeSpan(); t += step)
    {
        var startLocal = date.ToDateTime(TimeOnly.FromTimeSpan(t));
        var endLocal   = startLocal.AddMinutes(durationMinutes);
        if (endLocal.TimeOfDay > close.ToTimeSpan()) break;

        var startUtc = startLocal.ToUniversalTime();
        var endUtc   = endLocal.ToUniversalTime();

        var overlaps = taken.Any(a => startUtc < a.EndUtc && endUtc > a.StartUtc);
        if (!overlaps) slots.Add(TimeOnly.FromTimeSpan(t).ToString("HH:mm"));
    }
    return slots;
}

// helper to round up DateTime to the next interval
private static DateTime RoundUp(DateTime dt, TimeSpan interval)
{
    // Round dt up to the next multiple of interval
    long ticks = ((dt.Ticks + interval.Ticks - 1) / interval.Ticks) * interval.Ticks;
    return new DateTime(ticks, dt.Kind);
}



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingForm form)
    {
        var svc = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.ServiceId && s.IsActive);
        if (svc is null) return NotFound();

        IActionResult Back(string message)
        {
            TempData["Message"] = message; // shown by your _Layout flash
            return RedirectToAction(nameof(Create), new {
                serviceId   = form.ServiceId,
                date        = form.Date,
                selectedTime = form.Time
            });
        }

        if (!ModelState.IsValid)
        {
            // Example: phone failed regex, etc.
            return Back("Please check the form details and try again.");
        }

        if (!TimeOnly.TryParseExact(form.Time, "HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
        {
            return Back("That time value wasn’t valid. Please select a slot from the list.");
        }

        var localStart = form.Date.ToDateTime(t);
        var localEnd   = localStart.AddMinutes(svc.DurationMinutes);
        var startUtc   = localStart.ToUniversalTime();
        var endUtc     = localEnd.ToUniversalTime();

       // one-tech double-booking guard (blocks overlaps across ALL services)
        var conflict = await _db.Appointments.AnyAsync(a => 
            a.Status != Domain.AppointmentStatus.Cancelled &&
            startUtc < a.EndUtc && endUtc > a.StartUtc);


        if (conflict)
        {
            return Back("That time was just taken. Please choose another slot.");
        }

        var appt = new Appointment
        {
            ServiceId     = svc.Id,
            CustomerName  = form.CustomerName,
            CustomerEmail = form.CustomerEmail,
            CustomerPhone = form.CustomerPhone,
            StartUtc      = startUtc,
            EndUtc        = endUtc,
            Status        = AppointmentStatus.Pending,
            DepositPaid   = false
        };

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Confirm), new { id = appt.Id });
    }


    [HttpGet]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var appt = await _db.Appointments
            .Include(a => a.Service)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appt is null) return NotFound();
        return View(appt);
        
    }
}
