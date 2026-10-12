using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OdemeController : Controller
    {
        private readonly AlfabeContext _context;

        public OdemeController(AlfabeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? ogrenciAdi, string? odemeDurumu, DateTime? baslangicTarihi, DateTime? bitisTarihi, int page = 1)
        {
            int pageSize = 10; // Her sayfada gösterilecek kayıt sayısı
            var sorgu = _context.Odemeler.Include(o => o.Ogrenci).AsQueryable();

            if (!string.IsNullOrWhiteSpace(ogrenciAdi))
                sorgu = sorgu.Where(o => (o.Ogrenci.Ad + " " + o.Ogrenci.Soyad).Contains(ogrenciAdi));

            if (!string.IsNullOrEmpty(odemeDurumu))
            {
                if (odemeDurumu == "Odendi")
                    sorgu = sorgu.Where(o => o.OdendiMi);
                else if (odemeDurumu == "Odenmedi")
                    sorgu = sorgu.Where(o => !o.OdendiMi);
            }

            if (baslangicTarihi.HasValue)
                sorgu = sorgu.Where(o => o.OdemeTarihi >= baslangicTarihi.Value);

            if (bitisTarihi.HasValue)
                sorgu = sorgu.Where(o => o.OdemeTarihi <= bitisTarihi.Value);

            int totalRecords = await sorgu.CountAsync();

            var odemeler = await sorgu
                .OrderByDescending(o => o.OdemeTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new OdemeFiltrelemeViewModel
            {
                OgrenciAdi = ogrenciAdi,
                OdemeDurumu = odemeDurumu,
                BaslangicTarihi = baslangicTarihi,
                BitisTarihi = bitisTarihi,
                Odemeler = odemeler,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var odeme = await _context.Odemeler
                .Include(o => o.Ogrenci)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (odeme == null) return NotFound();

            return View(odeme);
        }

        public IActionResult Create()
        {
            ViewData["OgrenciId"] = new SelectList(
                _context.Ogrenciler
                    .Select(o => new { o.Id, AdSoyad = o.Ad + " " + o.Soyad })
                    .OrderBy(o => o.AdSoyad)
                    .ToList(),
                "Id", "AdSoyad"
            );
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Odeme odeme)
        {
            if (ModelState.IsValid)
            {
                _context.Add(odeme);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ödeme başarıyla kaydedildi.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Lütfen tüm zorunlu alanları doğru şekilde doldurun.";

            ViewData["OgrenciId"] = new SelectList(
                _context.Ogrenciler.Select(o => new { o.Id, AdSoyad = o.Ad + " " + o.Soyad }).OrderBy(o => o.AdSoyad).ToList(),
                "Id", "AdSoyad", odeme?.OgrenciId
            );

            return View(odeme);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var odeme = await _context.Odemeler.FindAsync(id);
            if (odeme == null) return NotFound();

            ViewData["OgrenciId"] = new SelectList(
                _context.Ogrenciler.Select(o => new { o.Id, AdSoyad = o.Ad + " " + o.Soyad }).OrderBy(o => o.AdSoyad).ToList(),
                "Id", "AdSoyad", odeme.OgrenciId
            );

            return View(odeme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Odeme odeme)
        {
            if (id != odeme.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(odeme);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ödeme başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OdemeExists(odeme.Id)) return NotFound();
                    throw;
                }
            }

            ViewData["OgrenciId"] = new SelectList(
                _context.Ogrenciler.Select(o => new { o.Id, AdSoyad = o.Ad + " " + o.Soyad }).OrderBy(o => o.AdSoyad).ToList(),
                "Id", "AdSoyad", odeme.OgrenciId
            );

            return View(odeme);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var odeme = await _context.Odemeler
                .Include(o => o.Ogrenci)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (odeme == null) return NotFound();

            return View(odeme);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var odeme = await _context.Odemeler.FindAsync(id);
            if (odeme != null)
            {
                _context.Odemeler.Remove(odeme);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ödeme silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OdemeExists(int id)
        {
            return _context.Odemeler.Any(e => e.Id == id);
        }
    }
}
