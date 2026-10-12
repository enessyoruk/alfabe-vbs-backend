using System.Diagnostics;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AlfabeContext _context;

        public HomeController(ILogger<HomeController> logger, AlfabeContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewData["OgrenciSayisi"] = await _context.Ogrenciler.CountAsync();
                ViewData["VeliSayisi"] = await _context.Veliler.CountAsync();
                ViewData["OgretmenSayisi"] = await _context.Ogretmenler.CountAsync();
                ViewData["DersSayisi"] = await _context.Dersler.CountAsync();
                ViewData["YoklamaSayisi"] = await _context.Yoklamalar.CountAsync();
                ViewData["OdemeSayisi"] = await _context.Odemeler.CountAsync();
            }
            else
            {
                ViewData["OgrenciSayisi"] = "-";
                ViewData["VeliSayisi"] = "-";
                ViewData["OgretmenSayisi"] = "-";
                ViewData["DersSayisi"] = "-";
                ViewData["YoklamaSayisi"] = "-";
                ViewData["OdemeSayisi"] = "-";
            }

            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }


        [AllowAnonymous]
        public IActionResult KVKK()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Gizlilik()
        {
            return View();
        }
    }
}
