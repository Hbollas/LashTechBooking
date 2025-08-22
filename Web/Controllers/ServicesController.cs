using Domain;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class ServicesController : Controller
{
    private readonly AppDbContext _db;
    public ServicesController(AppDbContext db) => _db = db;

    // GET: /Services?q=fill&sort=price_desc
    public async Task<IActionResult> Index(string? q, string? sort = "price")
    {
        IQueryable<Service> query = _db.Services.Where(s => s.IsActive);

        // simple search: name or description contains term
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.Name, $"%{term}%") ||
                (s.Description != null && EF.Functions.Like(s.Description, $"%{term}%")));
        }

        // sorting
        query = sort switch
        {
            "name"           => query.OrderBy(s => s.Name),
            "name_desc"      => query.OrderByDescending(s => s.Name),
            "duration"       => query.OrderBy(s => s.DurationMinutes),
            "duration_desc"  => query.OrderByDescending(s => s.DurationMinutes),
            "price_desc"     => query.OrderByDescending(s => s.PriceCents),
            _                => query.OrderBy(s => s.PriceCents) // default: price asc
        };

        var services = await query.AsNoTracking().ToListAsync();

        // pass current query state to the view (we'll use this next)
        ViewBag.CurrentSort = sort;
        ViewBag.Search = q;

        return View(services);
    }

    // GET: /Services/{id}
    [HttpGet("/Services/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var svc = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (svc is null) return NotFound();
        return View(svc); // we'll create this view after we wire booking
    }
}
