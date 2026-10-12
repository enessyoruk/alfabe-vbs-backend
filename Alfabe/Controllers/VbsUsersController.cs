using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Alfabe.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Tags("Admin")]
    public class VbsUsersController : Controller
    {
        private readonly AlfabeContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private const int PageSize = 10;

        public VbsUsersController(
            AlfabeContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1) page = 1;

            
            var query = _db.VbsUsers
                .Where(x => !x.IsApproved)
                .OrderByDescending(x => x.CreatedAt)
                .AsQueryable();

            var totalItems = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            ViewBag.CurrentPage = page;

            return View(users);
        }

        public async Task<IActionResult> ApprovedList(int page = 1)
        {
            if (page < 1) page = 1;

            var query = _db.VbsUsers
                .Where(x => x.IsApproved)
                .OrderByDescending(x => x.ApprovedAt)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var users = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            ViewBag.CurrentPage = page;

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var v = await _db.VbsUsers.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null)
            {
                TempData["Error"] = "Başvuru bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (v.IsApproved)
            {
                TempData["Info"] = $"{v.FullName} zaten onaylanmış.";
                return RedirectToAction(nameof(Index));
            }

            var email = (v.Email ?? "").Trim().ToLowerInvariant();
            var roleName =
                v.UserType?.Equals("parent", StringComparison.OrdinalIgnoreCase) == true ? "Parent" :
                v.UserType?.Equals("teacher", StringComparison.OrdinalIgnoreCase) == true ? "Teacher" :
                (string?)null;

            if (roleName == null)
            {
                TempData["Error"] = "Geçersiz kullanıcı tipi (parent/teacher).";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = "Bu e-posta zaten sistemde kayıtlı.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var appUser = new AppUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = v.PhoneNumber,
                    AdSoyad = v.FullName
                };

                var createRes = await _userManager.CreateAsync(appUser);
                if (!createRes.Succeeded)
                {
                    TempData["Error"] = string.Join("; ", createRes.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }

                appUser.PasswordHash = v.PasswordHash;
                var updateRes = await _userManager.UpdateAsync(appUser);
                if (!updateRes.Succeeded)
                {
                    TempData["Error"] = string.Join("; ", updateRes.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }

                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var roleCreate = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleCreate.Succeeded)
                    {
                        TempData["Error"] = "Rol oluşturulamadı.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var addRoleRes = await _userManager.AddToRoleAsync(appUser, roleName);
                if (!addRoleRes.Succeeded)
                {
                    TempData["Error"] = string.Join("; ", addRoleRes.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }

                if (await _userManager.IsInRoleAsync(appUser, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(appUser, "Admin");
                }

                var rawName = (v.FullName ?? "").Trim();
                var nameParts = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var firstName = nameParts.Length > 0 ? nameParts[0] : "Bilinmiyor";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : firstName;

                if (roleName == "Parent")
                {
                    var telNorm = (v.PhoneNumber ?? "").Trim();

                    Veli? veli = null;

                    if (!string.IsNullOrWhiteSpace(telNorm))
                    {
                        veli = await _db.Veliler.FirstOrDefaultAsync(x => x.Telefon == telNorm);
                    }

                    if (veli == null)
                    {
                        veli = await _db.Veliler
                            .FirstOrDefaultAsync(x => x.Ad == firstName && x.Soyad == lastName);
                    }

                    if (veli == null)
                    {
                        veli = new Veli
                        {
                            Ad = firstName,
                            Soyad = lastName,
                            Telefon = string.IsNullOrWhiteSpace(telNorm) ? null : telNorm,
                            Telefon2 = null,
                            AdresId = null,
                            AppUserId = appUser.Id // 🔥 KRİTİK
                        };

                        _db.Veliler.Add(veli);
                    }
                    else
                    {
                        veli.AppUserId = appUser.Id;

                        if (string.IsNullOrWhiteSpace(veli.Telefon) && !string.IsNullOrWhiteSpace(telNorm))
                            veli.Telefon = telNorm;

                        _db.Veliler.Update(veli);
                    }
                }

                if (roleName == "Teacher")
                {
                    var t = (v.PhoneNumber ?? "").Trim();

                    var ogretmen = new Ogretmen
                    {
                        Ad = firstName,
                        Soyad = lastName,
                        Telefon = string.IsNullOrWhiteSpace(t) ? "05000000000" : t,
                        Email = email,

                        AppUserId = appUser.Id
                    };

                    _db.Ogretmenler.Add(ogretmen);
                }

                v.IsApproved = true;
                v.ApprovedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                TempData["Success"] = $"{v.FullName} onaylandı ve kullanıcı sisteme eklendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Onay sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var v = await _db.VbsUsers.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null)
            {
                TempData["Error"] = "Başvuru bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (v.IsApproved)
            {
                TempData["Error"] = "Onaylanmış başvuru reddedilemez.";
                return RedirectToAction(nameof(Index));
            }

            
            v.IsRejected = true;
            v.RejectedAt = DateTime.UtcNow;

            _db.VbsUsers.Update(v);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{v.FullName} başvurusu reddedildi.";
            return RedirectToAction(nameof(Index));
        }

    }
}
