using Alfabe.Data;
using Alfabe.Models;
using Alfabe.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class YoklamaController : Controller
    {
        private readonly AlfabeContext _context;

        public YoklamaController(AlfabeContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetOgrencilerBySinif(int sinif)
        {
            var ogrenciler = _context.Ogrenciler
                .Where(o => o.Sinif == sinif && o.AktifMi)
                .Select(o => new
                {
                    id = o.Id,
                    adSoyad = o.Ad + " " + o.Soyad
                })
                .ToList();

            return Json(ogrenciler);
        }

        public async Task<IActionResult> Index(string search, int? devamDurumuId, string tarihAraligi, int page = 1)
        {
            int pageSize = 10;

            var birAyOnce = DateTime.Today.AddMonths(-1);
            var yoklamalar = _context.Yoklamalar
                .Include(y => y.Ogrenci)
                .Include(y => y.DevamDurumu)
                .Where(y => y.Tarih >= birAyOnce)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                yoklamalar = yoklamalar.Where(y => y.Ogrenci.Ad.Contains(search) || y.Ogrenci.Soyad.Contains(search));

            if (devamDurumuId.HasValue)
                yoklamalar = yoklamalar.Where(y => y.DevamDurumuId == devamDurumuId.Value);

            if (!string.IsNullOrEmpty(tarihAraligi))
            {
                var tarihler = tarihAraligi.Split(" - ");
                if (tarihler.Length == 2 &&
                    DateTime.TryParse(tarihler[0], out DateTime baslangic) &&
                    DateTime.TryParse(tarihler[1], out DateTime bitis))
                {
                    yoklamalar = yoklamalar.Where(y => y.Tarih >= baslangic && y.Tarih <= bitis);
                }
            }

            int totalRecords = await yoklamalar.CountAsync();
            var yoklamalarList = await yoklamalar
                .OrderByDescending(y => y.Tarih)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["SearchTerm"] = search;
            ViewData["DevamDurumuId"] = devamDurumuId;
            ViewData["TarihAraligi"] = tarihAraligi;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalRecords / (double)pageSize);

            ViewBag.DevamDurumlariListesi = new SelectList(_context.DevamDurumlari, "Id", "DurumAdi");

            return View(yoklamalarList);
        }

        public IActionResult Create()
        {
            var siniflar = _context.Ogrenciler
                .Where(o => o.AktifMi)
                .Select(o => o.Sinif)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            ViewBag.Siniflar = siniflar
                .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
                .ToList() ?? new List<SelectListItem>();

            var devamDurumlari = _context.DevamDurumlari
                .OrderBy(d => d.DurumAdi)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.DurumAdi })
                .ToList() ?? new List<SelectListItem>();

            ViewBag.DevamDurumlariListesi = devamDurumlari;

            var model = new YoklamaViewModel
            {
                Tarih = DateTime.Today // Varsayılan tarih bugünün tarihi
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(YoklamaViewModel model)
        {
            if (model.Tarih < DateTime.Today.AddMonths(-1))
            {
                ModelState.AddModelError("Tarih", "Yoklama tarihi 1 aydan eski olamaz.");
            }

            if (model.OgrenciSecimleri == null || !model.OgrenciSecimleri.Any())
            {
                ModelState.AddModelError("", "En az bir öğrenci seçmelisiniz.");
            }
            else
            {
                foreach (var s in model.OgrenciSecimleri)
                {
                    if (s.DevamDurumuId == 0)
                    {
                        ModelState.AddModelError("", "Tüm öğrenciler için devam durumu seçilmelidir.");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Siniflar = _context.Ogrenciler
                    .Where(o => o.AktifMi)
                    .Select(o => o.Sinif)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
                    .ToList();

                ViewBag.DevamDurumlariListesi = _context.DevamDurumlari
                    .OrderBy(d => d.DurumAdi)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.DurumAdi })
                    .ToList();

                return View(model);
            }

            foreach (var s in model.OgrenciSecimleri)
            {
                _context.Yoklamalar.Add(new Yoklama
                {
                    Tarih = model.Tarih,
                    OgrenciId = s.OgrenciId,
                    DevamDurumuId = s.DevamDurumuId
                });
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Message"] = "Yoklama(lar) başarıyla eklendi.";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu: " + ex.Message);

                ViewBag.Siniflar = _context.Ogrenciler
                    .Where(o => o.AktifMi)
                    .Select(o => o.Sinif)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
                    .ToList();

                ViewBag.DevamDurumlariListesi = _context.DevamDurumlari
                    .OrderBy(d => d.DurumAdi)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.DurumAdi })
                    .ToList();

                return View(model);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var yoklama = await _context.Yoklamalar
                .Include(y => y.Ogrenci)
                .Include(y => y.DevamDurumu)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (yoklama == null) return NotFound();

            return View(yoklama);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var yoklama = await _context.Yoklamalar
                .Include(y => y.Ogrenci)
                .Include(y => y.DevamDurumu)
                .FirstOrDefaultAsync(y => y.Id == id);

            if (yoklama == null) return NotFound();

            var model = new YoklamaViewModel
            {
                Id = yoklama.Id,
                Tarih = yoklama.Tarih,
                OgrenciId = yoklama.OgrenciId,
                DevamDurumuId = yoklama.DevamDurumuId,
                Sinif = yoklama.Ogrenci.Sinif,
                DevamDurumlariListesi = _context.DevamDurumlari
                                        .OrderBy(d => d.DurumAdi)
                                        .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.DurumAdi })
                                        .ToList(),
                OgrencilerListesi = _context.Ogrenciler
                                        .Where(o => o.AktifMi)
                                        .OrderBy(o => o.Ad)
                                        .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Ad + " " + o.Soyad })
                                        .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, YoklamaViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (model.Tarih < DateTime.Today.AddMonths(-1))
            {
                ModelState.AddModelError("Tarih", "Yoklama tarihi 1 aydan eski olamaz.");
            }

            if (model.DevamDurumuId == 0)
            {
                ModelState.AddModelError("DevamDurumuId", "Devam durumu seçmelisiniz.");
            }

            if (!ModelState.IsValid)
            {
                model.DevamDurumlariListesi = _context.DevamDurumlari
                                            .OrderBy(d => d.DurumAdi)
                                            .Select(d => new SelectListItem
                                            {
                                                Value = d.Id.ToString(),
                                                Text = d.DurumAdi
                                            })
                                            .ToList();

                model.OgrencilerListesi = _context.Ogrenciler
                                            .Where(o => o.AktifMi)
                                            .OrderBy(o => o.Ad)
                                            .Select(o => new SelectListItem
                                            {
                                                Value = o.Id.ToString(),
                                                Text = o.Ad + " " + o.Soyad
                                            })
                                            .ToList();

                return View(model);
            }

            var yoklama = await _context.Yoklamalar.FindAsync(id);
            if (yoklama == null)
                return NotFound();

            yoklama.Tarih = model.Tarih;
            yoklama.DevamDurumuId = model.DevamDurumuId; // tekli seçim için doğru alan

            try
            {
                _context.Update(yoklama);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Yoklama başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Yoklamalar.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var yoklama = await _context.Yoklamalar
                .Include(y => y.Ogrenci)
                .Include(y => y.DevamDurumu)
                .FirstOrDefaultAsync(y => y.Id == id);

            if (yoklama == null) return NotFound();

            return View(yoklama);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var yoklama = await _context.Yoklamalar.FindAsync(id);
            if (yoklama != null)
            {
                _context.Yoklamalar.Remove(yoklama);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Yoklama başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetAll()
        {
            try
            {
                var allYoklamalar = _context.Yoklamalar.ToList();

                if (allYoklamalar.Count > 0)
                {
                    _context.Yoklamalar.RemoveRange(allYoklamalar);
                    _context.SaveChanges();
                }

                TempData["Message"] = "Tüm yoklamalar başarıyla sıfırlandı.";
            }
            catch (Exception)
            {
                TempData["Message"] = "Yoklamalar sıfırlanırken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
