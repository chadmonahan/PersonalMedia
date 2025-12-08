using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalMedia.Data;
using PersonalMedia.Web.Models;

namespace PersonalMedia.Web.Controllers;

public class HomeController : Controller
{
    private readonly PersonalMediaDbContext _context;
    private readonly IConfiguration _configuration;

    public HomeController(PersonalMediaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SignIn()
    {
        if (HttpContext.Session.GetString("Authenticated") == "true")
        {
            return RedirectToAction(nameof(App));
        }
        return View();
    }

    [HttpPost]
    public IActionResult SignIn(string code)
    {
        var validCode = _configuration["AppSettings:AccessCode"] ?? "1234";

        if (code == validCode)
        {
            HttpContext.Session.SetString("Authenticated", "true");
            return RedirectToAction(nameof(App));
        }

        ViewBag.Error = "Invalid code. Please try again.";
        return View();
    }

    public IActionResult App()
    {
        if (HttpContext.Session.GetString("Authenticated") != "true")
        {
            return RedirectToAction(nameof(SignIn));
        }

        var mediaSets = _context.MediaSets
            .Include(s => s.MediaItems.OrderBy(m => m.DisplayOrder))
            .ThenInclude(m => m.Reactions)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedDate)
            .ToList();

        return View(mediaSets);
    }

    public new IActionResult SignOut()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
