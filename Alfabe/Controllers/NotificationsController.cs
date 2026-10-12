using System.ComponentModel.DataAnnotations;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers.Admin
{
    [Authorize(Roles = "Admin")] 
    public class NotificationsController : Controller
    {
        private readonly AlfabeContext _db;

        public NotificationsController(AlfabeContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? audience = null, bool? published = null)
        {
            var qry = _db.Notifications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(audience))
            {
                var a = NormalizeAudience(audience);
                if (a != null) qry = qry.Where(x => x.Audience == a);
            }

            if (published.HasValue)
            {
                qry = qry.Where(x => x.IsPublished == published.Value);
            }

            var items = await qry
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Audience = audience;
            ViewBag.Published = published;
            return View(items);
        }

        public IActionResult Create()
        {
            return View(new CreateVm { PublishNow = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            if (vm.PublishNow)
            {
                vm.ScheduledAt = null;
                vm.ExpiresAt = null;
            }

            var now = DateTimeOffset.UtcNow;

            var n = new Notification
            {
                Title = vm.Title.Trim(),
                Message = vm.Message.Trim(),
                Audience = vm.Audience,
                CreatedAt = now,
                ScheduledAt = vm.ScheduledAt,
                ExpiresAt = vm.ExpiresAt,
                IsPublished = vm.PublishNow
            };

            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Bildirim oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();

            n.IsPublished = true;
            await _db.SaveChangesAsync();

            TempData["ok"] = "Bildirim yayınlandı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();

            n.IsPublished = false;
            await _db.SaveChangesAsync();

            TempData["ok"] = "Bildirim yayından alındı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();

            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Bildirim silindi.";
            return RedirectToAction(nameof(Index));
        }

        private static NotificationAudience? NormalizeAudience(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "parents":
                case "veli":
                case "veliler":
                    return NotificationAudience.Parents;

                case "teachers":
                case "ogretmen":
                case "öğretmenler":
                    return NotificationAudience.Teachers;

                case "both":
                case "ikisi":
                case "hepsi":
                    return NotificationAudience.Both;

                default:
                    return null;
            }
        }

        public class CreateVm
        {
            [Required, StringLength(150)]
            public string Title { get; set; } = string.Empty;

            [Required, StringLength(2000)]
            public string Message { get; set; } = string.Empty;

            [Required]
            public NotificationAudience Audience { get; set; } = NotificationAudience.Parents;

            public DateTimeOffset? ScheduledAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }

            public bool PublishNow { get; set; } = true;
        }
    }
}
