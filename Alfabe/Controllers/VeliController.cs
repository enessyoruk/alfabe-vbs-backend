using Alfabe.Data;
using Alfabe.Helpers;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VeliController : Controller
    {
        private readonly AlfabeContext _context;
        private readonly UserManager<AppUser> _userManager;

        public VeliController(AlfabeContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Veliler
                .Include(v => v.Adres)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v =>
                    v.Ad.Contains(search) ||
                    v.Soyad.Contains(search) ||
                    (v.Adres != null && v.Adres.DetayAdres.Contains(search))
                );
            }

            query = sort switch
            {
                "ad_desc" => query.OrderByDescending(v => v.Ad),
                "soyad" => query.OrderBy(v => v.Soyad),
                "soyad_desc" => query.OrderByDescending(v => v.Soyad),
                _ => query.OrderBy(v => v.Ad),
            };

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;

            var veliler = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(veliler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var veli = await _context.Veliler
                .Include(v => v.Adres)
                .Include(v => v.AppUser)
                .Include(v => v.Ogrenciler)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (veli == null) return NotFound();

            return View(veli);
        }

        public IActionResult Create()
        {
            return View(new Veli
            {
                Ad = "",
                Soyad = "",
                Telefon = "",
                Telefon2 = "",
                Adres = new Adres()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Ad,Soyad,Telefon,Telefon2,Adres")] Veli veli,
            string Email,
            string Password,
            string PasswordConfirm)
        {
            if (Password != PasswordConfirm)
                ModelState.AddModelError("", "Şifreler uyuşmuyor.");

            if (!ModelState.IsValid)
            {
                ViewBag.Email = Email;
                return View(veli);
            }

            var user = new AppUser
            {
                UserName = Email.Trim().ToLower(),
                Email = Email.Trim().ToLower(),
                NormalizedUserName = Email.Trim().ToUpper(),
                NormalizedEmail = Email.Trim().ToUpper(),
                PhoneNumber = veli.Telefon?.Trim(),
                AdSoyad = $"{veli.Ad} {veli.Soyad}"
            };

            var createUser = await _userManager.CreateAsync(user, Password);

            if (!createUser.Succeeded)
            {
                foreach (var e in createUser.Errors)
                    ModelState.AddModelError("", e.Description);

                ViewBag.Email = Email;
                return View(veli);
            }

            await _userManager.AddToRoleAsync(user, "Parent");

            if (veli.Adres != null && !string.IsNullOrWhiteSpace(veli.Adres.DetayAdres))
            {
                _context.Adresler.Add(veli.Adres);
                await _context.SaveChangesAsync();
                veli.AdresId = veli.Adres.Id;
            }

            await VbsUserHelper.CreateOrApproveVbsUser(
                _context,
                fullName: user.AdSoyad,
                phone: veli.Telefon ?? "",
                email: user.Email!,
                userType: "parent",
                passwordHash: user.PasswordHash!
            );

            veli.AppUserId = user.Id;
            _context.Veliler.Add(veli);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Veli başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var veli = await _context.Veliler
                .Include(v => v.Adres)
                .Include(v => v.AppUser)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (veli == null) return NotFound();

            ViewBag.Email = veli.AppUser?.Email;

            return View(veli);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
     int id,
     [Bind("Id,Ad,Soyad,Telefon,Telefon2,AdresId,Adres,AppUserId")] Veli veli,
     string Email,
     string OldEmail)
        {
            if (id != veli.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Email = Email;
                return View(veli);
            }

            _context.Update(veli);

            var user = await _userManager.FindByIdAsync(veli.AppUserId);

            if (user != null)
            {
                string lowerEmail = Email.Trim().ToLower();
                string upperEmail = Email.Trim().ToUpper();

                user.Email = lowerEmail;
                user.UserName = lowerEmail;
                user.NormalizedEmail = upperEmail;
                user.NormalizedUserName = upperEmail;

                user.PhoneNumber = veli.Telefon?.Trim();
                user.AdSoyad = $"{veli.Ad} {veli.Soyad}";

                await _userManager.UpdateAsync(user);

                var oldLower = OldEmail?.Trim().ToLower();

                var vbsUser = await _context.VbsUsers
                    .FirstOrDefaultAsync(x => x.Email.ToLower() == oldLower);

                if (vbsUser != null)
                {
                    vbsUser.FullName = user.AdSoyad;
                    vbsUser.PhoneNumber = veli.Telefon ?? "";
                    vbsUser.Email = user.Email!;
                    vbsUser.PasswordHash = user.PasswordHash!;
                    vbsUser.UserType = "parent";
                    vbsUser.IsApproved = true;
                    vbsUser.ApprovedAt = DateTime.Now;
                }
                else
                {
                    await VbsUserHelper.CreateOrApproveVbsUser(
                        _context,
                        user.AdSoyad,
                        veli.Telefon ?? "",
                        user.Email!,
                        "parent",
                        user.PasswordHash!
                    );
                }

                await _userManager.UpdateSecurityStampAsync(user);
            }

            if (veli.Adres != null)
            {
                if (veli.Adres.Id == 0)
                {
                    _context.Adresler.Add(veli.Adres);
                    await _context.SaveChangesAsync();
                    veli.AdresId = veli.Adres.Id;
                }
                else
                {
                    _context.Adresler.Update(veli.Adres);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Veli başarıyla güncellendi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var veli = await _context.Veliler
                .Include(v => v.AppUser)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (veli?.AppUser == null)
            {
                TempData["HataMesaji"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var user = veli.AppUser;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["HataMesaji"] = "Şifre sıfırlanamadı.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            await VbsUserHelper.CreateOrApproveVbsUser(
                _context,
                user.AdSoyad,
                veli.Telefon ?? "",
                user.Email!,
                "parent",
                user.PasswordHash!
            );

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Message"] = "Yeni şifre oluşturuldu.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var veli = await _context.Veliler
                .Include(v => v.AppUser)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (veli == null) return NotFound();

            return View(veli);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var veli = await _context.Veliler
                .Include(v => v.AppUser)
                .Include(v => v.Ogrenciler)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (veli == null)
                return RedirectToAction(nameof(Index));

            foreach (var ogr in veli.Ogrenciler.ToList())
            {
                _context.Yoklamalar.RemoveRange(
                    _context.Yoklamalar.Where(y => y.OgrenciId == ogr.Id));

                _context.OgrenciDersler.RemoveRange(
                    _context.OgrenciDersler.Where(d => d.OgrenciId == ogr.Id));

                _context.Odemeler.RemoveRange(
                    _context.Odemeler.Where(o => o.OgrenciId == ogr.Id));

                _context.Ogrenciler.Remove(ogr);
            }

            if (veli.AppUserId != null)
            {
                var user = await _userManager.FindByIdAsync(veli.AppUserId);
                if (user != null)
                {
                    var vbsUser = await _context.VbsUsers
                        .FirstOrDefaultAsync(v => v.Email == user.Email);

                    if (vbsUser != null)
                        _context.VbsUsers.Remove(vbsUser);

                    await _userManager.DeleteAsync(user);
                }
            }

            _context.Veliler.Remove(veli);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Veli başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

    }
}
