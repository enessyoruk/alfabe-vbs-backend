using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Alfabe.Helpers;

namespace Alfabe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OgretmenController : Controller
    {
        private readonly AlfabeContext _context;
        private readonly UserManager<AppUser> _userManager;

        public OgretmenController(AlfabeContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Ogretmenler.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ogretmen = await _context.Ogretmenler.FirstOrDefaultAsync(m => m.Id == id);
            if (ogretmen == null) return NotFound();

            return View(ogretmen);
        }

        public IActionResult Create()
        {
            return View(new Ogretmen());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Ad,Soyad,Telefon,Email")] Ogretmen ogretmen,
            string Password,
            string PasswordConfirm)
        {
            if (!ModelState.IsValid)
                return View(ogretmen);

            if (Password != PasswordConfirm)
            {
                ModelState.AddModelError("", "Şifreler uyuşmuyor.");
                return View(ogretmen);
            }

            var phone = string.IsNullOrWhiteSpace(ogretmen.Telefon)
                ? "05000000000"
                : ogretmen.Telefon.Trim();

            var lowerEmail = ogretmen.Email.Trim().ToLower();
            var upperEmail = ogretmen.Email.Trim().ToUpper();

            var user = new AppUser
            {
                UserName = lowerEmail,
                NormalizedUserName = upperEmail,
                Email = lowerEmail,
                NormalizedEmail = upperEmail,
                PhoneNumber = phone,
                AdSoyad = $"{ogretmen.Ad} {ogretmen.Soyad}"
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(ogretmen);
            }

            await _userManager.AddToRoleAsync(user, "Teacher");

            await VbsUserHelper.CreateOrApproveVbsUser(
                _context,
                user.AdSoyad,
                phone,
                user.Email!,
                "teacher",
                user.PasswordHash!
            );

            ogretmen.AppUserId = user.Id;
            ogretmen.Telefon = phone;
            ogretmen.Email = lowerEmail;

            _context.Ogretmenler.Add(ogretmen);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Öğretmen başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ogretmen = await _context.Ogretmenler.FindAsync(id);
            if (ogretmen == null) return NotFound();

            ViewBag.Email = ogretmen.Email;

            return View(ogretmen);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    [Bind("Id,Ad,Soyad,Telefon,Email,AppUserId")] Ogretmen form,
    string OldEmail,
    string GeneratedPassword)
        {
            ModelState.Remove("GeneratedPassword");

            if (id != form.Id)
                return NotFound();

            var ogretmen = await _context.Ogretmenler.FindAsync(id);
            if (ogretmen == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(form);

            var user = await _userManager.FindByIdAsync(ogretmen.AppUserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Identity kullanıcısı bulunamadı.");
                return View(form);
            }

            var phone = string.IsNullOrWhiteSpace(form.Telefon)
                ? "05000000000"
                : form.Telefon.Trim();

            var lowerEmail = form.Email.Trim().ToLower();
            var upperEmail = form.Email.Trim().ToUpper();

            user.Email = lowerEmail;
            user.UserName = lowerEmail;
            user.NormalizedEmail = upperEmail;
            user.NormalizedUserName = upperEmail;

            var fullName = $"{form.Ad} {form.Soyad}";
            user.AdSoyad = fullName;
            user.PhoneNumber = phone;

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(GeneratedPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, GeneratedPassword);
            }

            ogretmen.Ad = form.Ad;
            ogretmen.Soyad = form.Soyad;
            ogretmen.Telefon = phone;
            ogretmen.Email = lowerEmail;

            var oldLower = OldEmail?.Trim().ToLower();

            var vbsUser = await _context.VbsUsers
                .FirstOrDefaultAsync(x => x.Email.ToLower() == oldLower);

            if (vbsUser != null)
            {
                vbsUser.FullName = fullName;
                vbsUser.PhoneNumber = phone;
                vbsUser.Email = lowerEmail;
                vbsUser.PasswordHash = user.PasswordHash!;
                vbsUser.UserType = "teacher";
                vbsUser.IsApproved = true;
                vbsUser.ApprovedAt = DateTime.Now;
            }
            else
            {
                await VbsUserHelper.CreateOrApproveVbsUser(
                    _context,
                    fullName,
                    phone,
                    lowerEmail,
                    "teacher",
                    user.PasswordHash!
                );
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Öğretmen başarıyla güncellendi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ogretmen = await _context.Ogretmenler
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ogretmen == null) return NotFound();

            return View(ogretmen);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
                return RedirectToAction(nameof(Index));

            var ogretmen = await _context.Ogretmenler.FindAsync(id);
            if (ogretmen == null)
                return RedirectToAction(nameof(Index));

            var teacherUserId = ogretmen.AppUserId;

            _context.DerslerOgretmen.RemoveRange(
                _context.DerslerOgretmen.Where(x => x.OgretmenId == id)
            );

            _context.Homeworks.RemoveRange(
                _context.Homeworks.Where(x => x.CreatedByTeacherId == id)
            );

            _context.GuidanceNotes.RemoveRange(
                _context.GuidanceNotes.Where(x => x.CreatedById == teacherUserId)
            );

            if (ogretmen.AppUserId != null)
            {
                var user = await _userManager.FindByIdAsync(ogretmen.AppUserId);
                if (user != null)
                {
                    var email = user.Email;
                    await _userManager.DeleteAsync(user);

                    var vbs = await _context.VbsUsers.FirstOrDefaultAsync(x => x.Email == email);
                    if (vbs != null)
                        _context.VbsUsers.Remove(vbs);
                }
            }

            _context.Ogretmenler.Remove(ogretmen);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Öğretmen başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

    }
}
