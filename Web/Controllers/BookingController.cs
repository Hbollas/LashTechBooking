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

    // GET: /Booking/Create?serviceId=...&date=2025-08-20
    [HttpGet]
    public async Task<IActionResult> Create(Guid serviceId, DateOnly? date)
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


        return View(vm);
    }

    // very simple slot generator: every 30 minutes between 10:00–18:00 local time
private async Task<List<string>> GenerateSlotsAsync(Guid serviceId, DateOnly date, int durationMinutes)
{
    // business hours (adjust as you like)
    var open  = new TimeOnly(9, 0);
    var close = new TimeOnly(19, 0);
    var step  = TimeSpan.FromMinutes(30);

    // compute "earliest allowed start" on this device
    var openLocal       = date.ToDateTime(open);
    var closeLocal      = date.ToDateTime(close);

    DateTime earliestStartLocal;
    if (date == DateOnly.FromDateTime(DateTime.Today))
    {
        // round 'now' up to next 30-min boundary, so 11:10 -> 11:30, 11:40 -> 12:00
        var now = DateTime.Now;
        earliestStartLocal = RoundUp(now, step);

        // but never earlier than opening time
        if (earliestStartLocal < openLocal) earliestStartLocal = openLocal;
    }
    else
    {
        earliestStartLocal = openLocal;
    }

    // get taken intervals for that day (in UTC)
    var earliestUtc = earliestStartLocal.ToUniversalTime();
    var endOfDayUtc = closeLocal.ToUniversalTime();

    var taken = await _db.Appointments
        .Where(a => a.ServiceId == serviceId && a.Status != AppointmentStatus.Cancelled)
        .Where(a => a.StartUtc < endOfDayUtc && a.EndUtc > earliestUtc)
        .Select(a => new { a.StartUtc, a.EndUtc })
        .ToListAsync();

    var slots = new List<string>();
    for (var t = open.ToTimeSpan(); t <= close.ToTimeSpan(); t += step)
    {
        var startLocal = date.ToDateTime(TimeOnly.FromTimeSpan(t));
        if (startLocal < earliestStartLocal) continue;      // hide past times for today

        var endLocal   = startLocal.AddMinutes(durationMinutes);
        if (endLocal.TimeOfDay > close.ToTimeSpan()) break; // must finish by closing

        var startUtc = startLocal.ToUniversalTime();
        var endUtc   = endLocal.ToUniversalTime();

        var overlaps = taken.Any(a => startUtc < a.EndUtc && endUtc > a.StartUtc);
        if (!overlaps)
        {
            // keep the value as "HH:mm" (parser expects this)
            slots.Add(TimeOnly.FromTimeSpan(t).ToString("HH:mm"));
        }
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
        var svc = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.ServiceId && s.IsActive);
        if (svc is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var vm = new CreateBookingVm
            {
                ServiceId = form.ServiceId,
                ServiceName = svc.Name,
                ServiceDurationMinutes = svc.DurationMinutes,
                Date = form.Date,
                TimeOptions = await GenerateSlotsAsync(svc.Id, form.Date, svc.DurationMinutes)
            };
            ViewBag.SelectedTime = form.Time;
            return View(vm);
        }

        if (!TimeOnly.TryParseExact(form.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
        {
            ModelState.AddModelError("Time", "Invalid time.");
            var vm = new CreateBookingVm
            {
                ServiceId = form.ServiceId,
                ServiceName = svc.Name,
                ServiceDurationMinutes = svc.DurationMinutes,
                Date = form.Date,
                TimeOptions = await GenerateSlotsAsync(svc.Id, form.Date, svc.DurationMinutes)
            };
            ViewBag.SelectedTime = form.Time;
            return View(vm);
        }

        var localStart = form.Date.ToDateTime(t);
        var localEnd = localStart.AddMinutes(svc.DurationMinutes);
        var startUtc = localStart.ToUniversalTime();
        var endUtc = localEnd.ToUniversalTime();

        var conflict = await _db.Appointments.AnyAsync(a =>
            a.ServiceId == svc.Id &&
            a.Status != Domain.AppointmentStatus.Cancelled &&
            startUtc < a.EndUtc && endUtc > a.StartUtc);

        if (conflict)
        {
            ModelState.AddModelError(string.Empty, "That time was just taken. Please choose another slot.");
            var vm = new CreateBookingVm
            {
                ServiceId = form.ServiceId,
                ServiceName = svc.Name,
                ServiceDurationMinutes = svc.DurationMinutes,
                Date = form.Date,
                TimeOptions = await GenerateSlotsAsync(svc.Id, form.Date, svc.DurationMinutes)
            };
            ViewBag.SelectedTime = form.Time;
            return View(vm);
        }

        var appt = new Domain.Appointment
        {
            ServiceId = svc.Id,
            CustomerName = form.CustomerName,
            CustomerEmail = form.CustomerEmail,
            CustomerPhone = form.CustomerPhone,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = Domain.AppointmentStatus.Pending,
            DepositPaid = false
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
