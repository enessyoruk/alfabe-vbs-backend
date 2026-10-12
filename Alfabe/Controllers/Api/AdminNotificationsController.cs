using System.ComponentModel.DataAnnotations;
using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers.Api
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Produces("application/json")]
    [Tags("Notifications")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly AlfabeContext _db;

        public AdminNotificationsController(AlfabeContext db)
        {
            _db = db;
        }

        public class NotificationCreateDto
        {
            [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
            [Required, StringLength(2000)] public string Message { get; set; } = string.Empty;
            [Required] public string Audience { get; set; } = "parents";
            public DateTimeOffset? ScheduledAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public bool PublishNow { get; set; } = true;
        }

        public class NotificationListItemDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Audience { get; set; } = "parents";
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? ScheduledAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public bool IsPublished { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] NotificationCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz istek." });

            var audience = NormalizeAudience(dto.Audience);
            if (audience is null)
                return BadRequest(new { error = "Audience geçersiz. (parents|teachers|both)" });

            var now = DateTimeOffset.UtcNow;

            if (dto.PublishNow)
            {
                dto.ScheduledAt = null;
                dto.ExpiresAt = null;
            }

            var n = new Notification
            {
                Title = dto.Title.Trim(),
                Message = dto.Message.Trim(),
                Audience = audience.Value,
                CreatedAt = now,
                ScheduledAt = dto.ScheduledAt,
                ExpiresAt = dto.ExpiresAt,
                IsPublished = true
            };

            _db.Notifications.Add(n);
            await _db.SaveChangesAsync(ct);

            return Created(string.Empty, ToListDto(n));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> List([FromQuery] string? audience = null, [FromQuery] bool? published = null, CancellationToken ct = default)
        {
            var qry = _db.Notifications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(audience))
            {
                var aud = NormalizeAudience(audience!);
                if (aud is null) return BadRequest(new { error = "Audience geçersiz. (parents|teachers|both)" });
                qry = qry.Where(x => x.Audience == aud);
            }

            if (published.HasValue)
                qry = qry.Where(x => x.IsPublished == published.Value);

            var items = await qry
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => ToListDto(x))
                .ToListAsync(ct);

            return Ok(new { items });
        }

        [HttpPut("{id:int}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Publish([FromRoute] int id, CancellationToken ct)
        {
            var n = await _db.Notifications.FindAsync(new object?[] { id }, ct);
            if (n is null) return NotFound(new { error = "Bildirim bulunamadı." });

            n.IsPublished = true;
            await _db.SaveChangesAsync(ct);
            return Ok(new { success = true });
        }

        [HttpPut("{id:int}/unpublish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Unpublish([FromRoute] int id, CancellationToken ct)
        {
            var n = await _db.Notifications.FindAsync(new object?[] { id }, ct);
            if (n is null) return NotFound(new { error = "Bildirim bulunamadı." });

            n.IsPublished = false;
            await _db.SaveChangesAsync(ct);
            return Ok(new { success = true });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            var n = await _db.Notifications.FindAsync(new object?[] { id }, ct);
            if (n is null) return NotFound(new { error = "Bildirim bulunamadı." });

            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync(ct);
            return Ok(new { success = true });
        }

        [HttpGet("~/api/vbs/parent/notifications")]
        [AllowAnonymous]
        public async Task<IActionResult> ParentFeed(CancellationToken ct)
        {
            var now = DateTimeOffset.Now;

            var items = await _db.Notifications
                .Where(x =>
                    x.IsPublished &&
                    (x.Audience == NotificationAudience.Parents || x.Audience == NotificationAudience.Both) &&
                    (x.ScheduledAt == null || x.ScheduledAt <= now) &&
                    (x.ExpiresAt == null || x.ExpiresAt >= now))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    message = x.Message,
                    type = "announcement",
                    date = x.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(new { items });
        }

        [HttpGet("~/api/vbs/teacher/notifications")]
        [AllowAnonymous]
        public async Task<IActionResult> TeacherFeed(CancellationToken ct)
        {
            var now = DateTimeOffset.Now;

            var items = await _db.Notifications
                .Where(x =>
                    x.IsPublished &&
                    (x.Audience == NotificationAudience.Teachers || x.Audience == NotificationAudience.Both) &&
                    (x.ScheduledAt == null || x.ScheduledAt <= now) &&
                    (x.ExpiresAt == null || x.ExpiresAt >= now))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    message = x.Message,
                    type = "announcement",
                    date = x.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(new { items });
        }

        private static NotificationListItemDto ToListDto(Notification x) => new NotificationListItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Message = x.Message,
            Audience = x.Audience.ToString().ToLowerInvariant(),
            CreatedAt = x.CreatedAt,
            ScheduledAt = x.ScheduledAt,
            ExpiresAt = x.ExpiresAt,
            IsPublished = x.IsPublished
        };

        private static NotificationAudience? NormalizeAudience(string audience)
        {
            switch ((audience ?? "").Trim().ToLowerInvariant())
            {
                case "parents": return NotificationAudience.Parents;
                case "teachers": return NotificationAudience.Teachers;
                case "both": return NotificationAudience.Both;
                default: return null;
            }
        }
    }
}
