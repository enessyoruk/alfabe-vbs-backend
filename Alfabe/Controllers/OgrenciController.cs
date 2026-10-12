using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OgrenciController : Controller
    {
        private readonly AlfabeContext _context;
        private const int pageSize = 10;

        public OgrenciController(AlfabeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1)
        {
            var query = _context.Ogrenciler.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o => o.Ad.Contains(search) || o.Soyad.Contains(search));
            }

            query = sortOrder switch
            {
                "ad_asc" => query.OrderBy(o => o.Ad),
                "ad_desc" => query.OrderByDescending(o => o.Ad),
                "soyad_asc" => query.OrderBy(o => o.Soyad),
                "soyad_desc" => query.OrderByDescending(o => o.Soyad),
                _ => query.OrderBy(o => o.Id),
            };

            int totalItems = await query.CountAsync();

            var ogrenciler = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["CurrentPage"] = page;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = search;

            return View(ogrenciler);
        }

        public IActionResult Create()
        {
            ViewData["VeliId"] = new SelectList(
    _context.Veliler
        .OrderBy(v => v.Ad)
        .ThenBy(v => v.Soyad)
        .Select(v => new { v.Id, AdSoyad = v.Ad + " " + v.Soyad }),
    "Id",
    "AdSoyad"
);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Ad,Soyad,Telefon,VeliId,Sinif")] Ogrenci ogrenci)
        {
            if (!ModelState.IsValid)
            {
                ViewData["VeliId"] = new SelectList(
                    _context.Veliler.Select(v => new { v.Id, AdSoyad = v.Ad + " " + v.Soyad }),
                    "Id",
                    "AdSoyad",
                    ogrenci.VeliId);

                return View(ogrenci);
            }

            _context.Add(ogrenci);
            await _context.SaveChangesAsync();

            await SyncOgrenciDerslerAsync(ogrenci);

            TempData["Message"] = "Öğrenci başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ogrenci = await _context.Ogrenciler.FindAsync(id);
            if (ogrenci == null) return NotFound();

            ViewData["VeliId"] = new SelectList(
    _context.Veliler
        .OrderBy(v => v.Ad)
        .ThenBy(v => v.Soyad)
        .Select(v => new { v.Id, AdSoyad = v.Ad + " " + v.Soyad }),
    "Id",
    "AdSoyad",
    ogrenci.VeliId
);

            return View(ogrenci);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ad,Soyad,Telefon,VeliId,Sinif")] Ogrenci ogrenci)
        {
            if (id != ogrenci.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["VeliId"] = new SelectList(
                    _context.Veliler.Select(v => new { v.Id, AdSoyad = v.Ad + " " + v.Soyad }),
                    "Id",
                    "AdSoyad",
                    ogrenci.VeliId);

                return View(ogrenci);
            }

            var eskiSinif = await _context.Ogrenciler
                .Where(o => o.Id == ogrenci.Id)
                .Select(o => o.Sinif)
                .FirstOrDefaultAsync();

            _context.Update(ogrenci);
            await _context.SaveChangesAsync();

            if (ogrenci.Sinif != eskiSinif)
            {
                await SyncOgrenciDerslerAsync(ogrenci);
            }

            TempData["Message"] = "Öğrenci başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ogrenci = await _context.Ogrenciler
                .Include(o => o.Veli)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ogrenci == null) return NotFound();

            return View(ogrenci);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ogrenci = await _context.Ogrenciler.FirstOrDefaultAsync(o => o.Id == id);
            if (ogrenci == null) return NotFound();

            _context.Yoklamalar.RemoveRange(
                _context.Yoklamalar.Where(y => y.OgrenciId == id));

            _context.OgrenciDersler.RemoveRange(
                _context.OgrenciDersler.Where(d => d.OgrenciId == id));

            _context.Odemeler.RemoveRange(
                _context.Odemeler.Where(o => o.OgrenciId == id));

            _context.ExamResults.RemoveRange(
                _context.ExamResults.Where(e => e.OgrenciId == id));

            _context.GuidanceNotes.RemoveRange(
                _context.GuidanceNotes.Where(g => g.StudentId == id));

            _context.Ogrenciler.Remove(ogrenci);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Öğrenci başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task SyncOgrenciDerslerAsync(Ogrenci ogrenci)
        {
            if (ogrenci == null || ogrenci.Sinif <= 0) return;

            var existingLinks = await _context.OgrenciDersler
                .Where(od => od.OgrenciId == ogrenci.Id)
                .ToListAsync();

            _context.OgrenciDersler.RemoveRange(existingLinks);

            var dersler = await _context.Dersler
                .Where(d => d.SinifSeviyesi == ogrenci.Sinif)
                .ToListAsync();

            foreach (var ders in dersler)
            {
                _context.OgrenciDersler.Add(new OgrenciDers
                {
                    OgrenciId = ogrenci.Id,
                    DersId = ders.Id
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ogrenci = await _context.Ogrenciler
                .Include(o => o.Veli)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ogrenci == null) return NotFound();

            return View(ogrenci);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAllStudentLessons()
        {
            var students = await _context.Ogrenciler.ToListAsync();

            foreach (var st in students)
            {
                await SyncOgrenciDerslerAsync(st);
            }

            TempData["Message"] = "Tüm öğrencilerin dersleri sınıflarıyla yeniden eşitlendi.";
            return RedirectToAction(nameof(Index));
        }

    }
}
