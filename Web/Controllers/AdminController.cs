using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Web.Models;
using Domain;
using System.Text;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;

    public AdminController(AppDbContext db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    // /Admin?From=2025-08-21&To=2025-08-28&Status=Pending
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminFilterVm f)
    {
        // sensible defaults: upcoming 14 days, all statuses
        var today = DateOnly.FromDateTime(DateTime.Today);
        f.From ??= today;
        f.To ??= today.AddDays(14);

        var fromUtc = f.From.Value.ToDateTime(new TimeOnly(0, 0)).ToUniversalTime();
        var toUtc = f.To.Value.ToDateTime(new TimeOnly(23, 59)).ToUniversalTime();

        var q = _db.Appointments.Include(a => a.Service).AsQueryable();
        q = q.Where(a => a.StartUtc >= fromUtc && a.StartUtc <= toUtc);

        if (f.Status.HasValue) q = q.Where(a => a.Status == f.Status.Value);

        f.Results = await q.OrderBy(a => a.StartUtc).Take(500).AsNoTracking().ToListAsync();
        return View(f);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var appt = await _db.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt is null) return NotFound();

        appt.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync();

        await SendApprovedEmail(appt);
        TempData["Message"] = "Appointment approved and email sent.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var appt = await _db.Appointments.Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appt is null) return NotFound();

        appt.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        var local = ToLocal(appt.StartUtc);
        var serviceName = appt.Service?.Name ?? "your service";
        var body =
            $"Hi {HtmlEncode(appt.CustomerName)},<br/>" +
            $"Your appointment for <b>{HtmlEncode(serviceName)}</b> " +
            $"on <b>{local:f}</b> has been cancelled.";
        await _email.SendEmailAsync(appt.CustomerEmail, "Appointment cancelled", body);

        TempData["Message"] = "Appointment cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDeposit(Guid id)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.DepositPaid = !appt.DepositPaid;
        await _db.SaveChangesAsync();

        TempData["Message"] = appt.DepositPaid ? "Deposit marked PAID." : "Deposit marked UNPAID.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SendApprovedEmail(Domain.Appointment appt)
    {
        var local = ToLocal(appt.StartUtc);
        var serviceName = appt.Service?.Name ?? "your service";
        var duration = appt.Service?.DurationMinutes ?? 0;

        var body =
            $"Hi {HtmlEncode(appt.CustomerName)},<br/>" +
            $"Great news—your appointment for <b>{HtmlEncode(serviceName)}</b> is <b>confirmed</b>.<br/>" +
            $"<b>When:</b> {local:f}<br/>" +
            (duration > 0 ? $"<b>Duration:</b> {duration} min<br/><br/>" : "<br/>") +
            $"See you soon!";


        await _email.SendEmailAsync(appt.CustomerEmail, "Your appointment is confirmed", body);
    }

    private static DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);

    private static string HtmlEncode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
