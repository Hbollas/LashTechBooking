using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class GalleryController : Controller
{
    private readonly IWebHostEnvironment _env;
    public GalleryController(IWebHostEnvironment env) => _env = env;

    public IActionResult Index()
    {
        var dir = Path.Combine(_env.WebRootPath, "images", "gallery");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg",".jpeg",".png",".webp" };

        var files = Directory.EnumerateFiles(dir)
            .Where(f => allow.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .Select(f => "/images/gallery/" + Path.GetFileName(f))
            .ToList();

        return View(files);
    }
}
