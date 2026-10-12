using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DerslerController : Controller
    {
        private readonly AlfabeContext _context;

        public DerslerController(AlfabeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string sortOrder)
        {
            var dersler = _context.Dersler.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                dersler = dersler.Where(d =>
                    d.DersAdi.Contains(search) ||
                    (d.Aciklama ?? "").Contains(search));
            }

            dersler = sortOrder switch
            {
                "dersadi_asc" => dersler.OrderBy(d => d.DersAdi),
                "dersadi_desc" => dersler.OrderByDescending(d => d.DersAdi),
                "aciklama_asc" => dersler.OrderBy(d => d.Aciklama),
                "aciklama_desc" => dersler.OrderByDescending(d => d.Aciklama),
                _ => dersler.OrderBy(d => d.Id)
            };

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentSort"] = sortOrder;

            return View(await dersler.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ders = await _context.Dersler
                .Include(d => d.DerslerOgretmen)
                    .ThenInclude(rel => rel.Ogretmen)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (ders == null) return NotFound();

            ders.Ogretmenler = ders.DerslerOgretmen
                .Where(x => x.Ogretmen != null)
                .Select(x => x.Ogretmen)
                .ToList();

            return View(ders);
        }

        public IActionResult Create()
        {
            PopulateOgretmenlerMultiSelect();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DersAdi,Aciklama,SinifSeviyesi")] Dersler ders, int[] selectedOgretmenler)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ders);
                await _context.SaveChangesAsync();

                if (selectedOgretmenler?.Any() == true)
                {
                    foreach (var ogretmenId in selectedOgretmenler)
                    {
                        _context.DerslerOgretmen.Add(new DerslerOgretmen
                        {
                            DersId = ders.Id,
                            OgretmenId = ogretmenId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                if (ders.SinifSeviyesi.HasValue)
                {
                    var ilgiliOgrenciler = await _context.Ogrenciler
                        .Where(o => o.Sinif == ders.SinifSeviyesi.Value && o.AktifMi)
                        .ToListAsync();

                    foreach (var ogrenci in ilgiliOgrenciler)
                    {
                        bool exists = await _context.OgrenciDersler
                            .AnyAsync(od => od.OgrenciId == ogrenci.Id && od.DersId == ders.Id);

                        if (!exists)
                        {
                            _context.OgrenciDersler.Add(new OgrenciDers
                            {
                                OgrenciId = ogrenci.Id,
                                DersId = ders.Id
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Ders başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }

            PopulateOgretmenlerMultiSelect(selectedOgretmenler);
            return View(ders);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ders = await _context.Dersler.FirstOrDefaultAsync(d => d.Id == id);
            if (ders == null) return NotFound();

            var selectedIds = await _context.DerslerOgretmen
                .Where(x => x.DersId == ders.Id)
                .Select(x => x.OgretmenId)
                .ToListAsync();

            PopulateOgretmenlerMultiSelect(selectedIds);

            return View(ders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,DersAdi,Aciklama,SinifSeviyesi")] Dersler ders,
            [FromForm] int[] selectedOgretmenler)
        {
            if (id != ders.Id)
                return NotFound();

            selectedOgretmenler = selectedOgretmenler ?? Array.Empty<int>();

            selectedOgretmenler = selectedOgretmenler
                .Where(x => x > 0)
                .ToArray();

            if (ModelState.IsValid)
            {
                var dersToUpdate = await _context.Dersler.FirstOrDefaultAsync(d => d.Id == id);
                if (dersToUpdate == null)
                    return NotFound();

                dersToUpdate.DersAdi = ders.DersAdi;
                dersToUpdate.Aciklama = ders.Aciklama;
                dersToUpdate.SinifSeviyesi = ders.SinifSeviyesi;

                var eski = await _context.DerslerOgretmen
                    .Where(x => x.DersId == id)
                    .ToListAsync();

                _context.DerslerOgretmen.RemoveRange(eski);

                if (selectedOgretmenler.Any())
                {
                    foreach (var ogretmenId in selectedOgretmenler)
                    {
                        _context.DerslerOgretmen.Add(new DerslerOgretmen
                        {
                            DersId = id,
                            OgretmenId = ogretmenId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Ders başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            PopulateOgretmenlerMultiSelect(selectedOgretmenler);
            return View(ders);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ders = await _context.Dersler.FirstOrDefaultAsync(d => d.Id == id);
            return ders == null ? NotFound() : View(ders);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ders = await _context.Dersler.FindAsync(id);

            if (ders != null)
            {
                _context.Dersler.Remove(ders);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Ders başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateOgretmenlerMultiSelect(IEnumerable<int>? selectedIds = null)
        {
            var rawList = _context.Ogretmenler
                .OrderBy(o => o.Ad)
                .ThenBy(o => o.Soyad)
                .Select(o => new
                {
                    Id = o.Id,
                    FullName = o.Ad + " " + o.Soyad
                })
                .ToList();

            var list = rawList
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.FullName,
                    Selected = selectedIds != null && selectedIds.Contains(o.Id)
                })
                .ToList();

            ViewBag.Ogretmenler = list;
        }

        private bool DersExists(int id)
        {
            return _context.Dersler.Any(e => e.Id == id);
        }
    }
}
