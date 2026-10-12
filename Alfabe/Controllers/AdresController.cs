using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdresController : Controller
    {
        private readonly AlfabeContext _context;

        public AdresController(AlfabeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var adresler = _context.Adresler.AsNoTracking();
            return View(await adresler.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var adres = await _context.Adresler.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return adres == null ? NotFound() : View(adres);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DetayAdres")] Adres adres)
        {
            if (ModelState.IsValid)
            {
                _context.Add(adres);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(adres);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var adres = await _context.Adresler.FindAsync(id);
            if (adres == null) return NotFound();

            return View(adres);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DetayAdres")] Adres adres)
        {
            if (id != adres.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(adres);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdresExists(adres.Id)) return NotFound();
                    throw;
                }
            }
            return View(adres);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var adres = await _context.Adresler.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return adres == null ? NotFound() : View(adres);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adres = await _context.Adresler.FindAsync(id);
            if (adres != null)
            {
                _context.Adresler.Remove(adres);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdresExists(int id)
        {
            return _context.Adresler.Any(e => e.Id == id);
        }
    }
}
