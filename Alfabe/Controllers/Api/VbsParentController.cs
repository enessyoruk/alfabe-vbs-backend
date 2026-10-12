using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace Alfabe.Controllers.Api
{
    [ApiController]
    [Route("api/vbs/parent")]
    [Produces("application/json")]
    [Tags("VBS")]
    public class VbsParentController : ControllerBase
    {
        private readonly AlfabeContext _db;
        private readonly IConfiguration _config;

        public VbsParentController(AlfabeContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private (string? userId, string? fullName, IList<string> roles) GetCurrentParent()
        {
            try
            {
                if (!Request.Cookies.TryGetValue("vbs_session", out var token) ||
                    string.IsNullOrWhiteSpace(token))
                {
                    return (null, null, new List<string>());
                }

                var secret =
                    _config["Jwt:SessionSecret"] ??
                    _config["SESSION_SECRET"];

                if (string.IsNullOrWhiteSpace(secret))
                    return (null, null, new List<string>());

                var key = Encoding.UTF8.GetBytes(secret);
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                tokenHandler.ValidateToken(token, validationParams, out var validated);
                var jwt = (JwtSecurityToken)validated;

                var uid =
                    jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ??
                    jwt.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

                var fullName = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                var rawRoleClaims = jwt.Claims
                    .Where(c =>
                        c.Type == "roles" ||
                        c.Type == "role" ||
                        c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                var roles = new List<string>();

                foreach (var roleStr in rawRoleClaims)
                {
                    if (string.IsNullOrWhiteSpace(roleStr)) continue;

                    if (roleStr.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            var parsed = System.Text.Json.JsonSerializer
                                .Deserialize<List<string>>(roleStr) ?? new List<string>();
                            roles.AddRange(parsed);
                        }
                        catch
                        {
                            roles.Add(roleStr);
                        }
                    }
                    else
                    {
                        roles.Add(roleStr);
                    }
                }

                return (uid, fullName, roles);
            }
            catch
            {
                return (null, null, new List<string>());
            }
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var (userId, _, roles) = GetCurrentParent();

            if (string.IsNullOrWhiteSpace(userId) ||
                !roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase)))
            {
                return Ok(new { items = Array.Empty<object>(), count = 0 });
            }

            var veli = await _db.Veliler
                .Include(v => v.Ogrenciler)
                .FirstOrDefaultAsync(v => v.AppUserId == userId);

            if (veli == null || veli.Ogrenciler == null || !veli.Ogrenciler.Any())
                return Ok(new { items = Array.Empty<object>(), count = 0 });

            var studentIds = veli.Ogrenciler
                .Select(o => o.Id)
                .Distinct()
                .ToList();

            if (!studentIds.Any())
                return Ok(new { items = Array.Empty<object>(), count = 0 });

            var studentIdSet = new HashSet<int>(studentIds);

            var yoklamalarAll = await _db.Yoklamalar
                .AsNoTracking()
                .ToListAsync();

            var dersKayitlariAll = await _db.OgrenciDersler
                .AsNoTracking()
                .Include(x => x.Ders)
                .ToListAsync();

            var dersOgretmen = await _db.DerslerOgretmen
                .AsNoTracking()
                .Include(x => x.Ogretmen)
                .ToListAsync();

            var yoklamalar = yoklamalarAll
                .Where(y => studentIdSet.Contains(y.OgrenciId))
                .ToList();

            var dersKayitlari = dersKayitlariAll
                .Where(x => studentIdSet.Contains(x.OgrenciId))
                .ToList();

            var ogrenciler = veli.Ogrenciler
                .OrderBy(o => o.Ad)
                .ToList();

            var result = ogrenciler.Select(o =>
            {
                var yList = yoklamalar
                    .Where(y => y.OgrenciId == o.Id)
                    .ToList();

                int attendanceRate = 0;
                int totalAbsences = 0;

                var valid = yList
                    .Where(y =>
                        y.DevamDurumuId == 1 ||
                        y.DevamDurumuId == 2 ||
                        y.DevamDurumuId == 3)
                    .ToList();

                if (valid.Count > 0)
                {
                    var present = valid.Count(y => y.DevamDurumuId == 1);
                    var absent = valid.Count(y => y.DevamDurumuId == 2);

                    attendanceRate = (int)Math.Round(100.0 * present / valid.Count);
                    totalAbsences = absent;
                }

                var dersler = dersKayitlari
                    .Where(d => d.OgrenciId == o.Id)
                    .Select(d => new
                    {
                        name = d.Ders?.DersAdi,
                        teacherName = string.Join(", ",
                            dersOgretmen
                                .Where(x => x.DersId == d.DersId)
                                .Select(x =>
                                    ((x.Ogretmen.Ad ?? "") + " " + (x.Ogretmen.Soyad ?? "")).Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t)))
                    })
                    .ToList();

                return new
                {
                    id = o.Id,
                    name = (o.Ad ?? "") + " " + (o.Soyad ?? ""),
                    fullName = (o.Ad ?? "") + " " + (o.Soyad ?? ""),
                    className = o.Sinif != null ? $"{o.Sinif}. Sınıf" : "Sınıf bilgisi yok",
                    @class = o.Sinif != null ? $"{o.Sinif}. Sınıf" : "Sınıf bilgisi yok",
                    branch = o.Sinif,
                    photo = (string?)null,
                    attendance = attendanceRate,
                    totalAbsences,
                    pendingHomework = 0,
                    lastExam = (string?)null,
                    subjects = dersler
                };
            })
            .ToList();

            return Ok(new { items = result, count = result.Count });
        }

        [HttpGet("students/{studentId:int}/attendance")]
        public async Task<IActionResult> GetStudentAttendance(int studentId, [FromQuery] string? month = null)
        {
            var (userId, _, roles) = GetCurrentParent();
            if (userId == null ||
                !roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase)))
                return Unauthorized(new { error = "Yetkisiz erişim" });

            var query = _db.Yoklamalar.Where(y => y.OgrenciId == studentId);

            if (!string.IsNullOrWhiteSpace(month) &&
                Regex.IsMatch(month, @"^\d{4}-\d{2}$"))
            {
                var year = int.Parse(month[..4]);
                var mon = int.Parse(month[5..7]);
                var start = new DateTime(year, mon, 1);
                var end = start.AddMonths(1);

                query = query.Where(y => y.Tarih >= start && y.Tarih < end);
            }

            var list = await query.OrderBy(y => y.Tarih).ToListAsync();

            var mapped = list.Select(y => new
            {
                id = y.Id,
                date = y.Tarih,
                course = "Genel Yoklama",
                status = y.DevamDurumuId == 1 ? "present" :
                         y.DevamDurumuId == 2 ? "absent" : "late",
                teacher = "Alfa-β Akademi",
                notes = (string?)null
            });

            return Ok(new { items = mapped, count = list.Count });
        }

        [HttpGet("students/{studentId:int}/attendance/stats")]
        public async Task<IActionResult> GetStudentAttendanceStats(int studentId, [FromQuery] string? month = null)
        {
            var (userId, _, roles) = GetCurrentParent();
            if (userId == null ||
                !roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase)))
                return Unauthorized(new { error = "Yetkisiz erişim" });

            var query = _db.Yoklamalar.Where(y => y.OgrenciId == studentId);

            if (!string.IsNullOrWhiteSpace(month) && month.Length == 7)
            {
                if (DateTime.TryParse($"{month}-01", out var d))
                {
                    var start = new DateTime(d.Year, d.Month, 1);
                    var end = start.AddMonths(1);

                    query = query.Where(y => y.Tarih >= start && y.Tarih < end);
                }
            }

            var list = await query.ToListAsync();

            var valid = list.Where(y => y.DevamDurumuId == 1 || y.DevamDurumuId == 2).ToList();

            var total = valid.Count;
            var present = valid.Count(y => y.DevamDurumuId == 1);
            var absent = valid.Count(y => y.DevamDurumuId == 2);
            var late = 0;

            var rate = total > 0 ? (int)Math.Round(100.0 * present / total) : 0;

            return Ok(new { total, present, absent, late, attendanceRate = rate });
        }

        [HttpGet("exams")]
        public async Task<IActionResult> GetParentExams()
        {
            var (userId, _, roles) = GetCurrentParent();

            if (userId == null ||
                !roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase)))
                return Ok(new { examResults = Array.Empty<object>() });

            var parent = await _db.Veliler
                .Include(v => v.Ogrenciler)
                .FirstOrDefaultAsync(v =>
                    v.AppUserId != null &&
                    v.AppUserId.ToLower() == userId.ToLower());

            if (parent == null)
                return Ok(new { examResults = Array.Empty<object>() });

            var studentIds = parent.Ogrenciler.Select(o => o.Id).ToList();
            if (!studentIds.Any())
                return Ok(new { examResults = Array.Empty<object>() });

            var studentIdSet = new HashSet<int>(studentIds);

            var exams = await _db.GeneralExams
                .AsNoTracking()
                .ToListAsync();

            var dersler = await _db.Dersler
                .AsNoTracking()
                .ToListAsync();

            var ogrDersler = await _db.OgrenciDersler
                .AsNoTracking()
                .ToListAsync();

            var ogrenciler = await _db.Ogrenciler
                .AsNoTracking()
                .ToListAsync();

            var ogretmenler = await _db.Ogretmenler
                .AsNoTracking()
                .ToListAsync();

            var analyses = await _db.GeneralExamAnalyses
                .AsNoTracking()
                .ToListAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var resultRows = new List<object>();

            foreach (var od in ogrDersler)
            {
                if (!studentIdSet.Contains(od.OgrenciId))
                    continue;

                var ogr = ogrenciler.FirstOrDefault(o => o.Id == od.OgrenciId);
                if (ogr == null)
                    continue;

                var ders = dersler.FirstOrDefault(d => d.Id == od.DersId);

                var examList = exams.Where(e => e.DersId == od.DersId).ToList();

                foreach (var ge in examList)
                {
                    var teacher = ogretmenler.FirstOrDefault(t => t.Id == ge.TeacherId);

                    var summary = analyses
                        .Where(a => a.GeneralExamId == ge.Id)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.Summary)
                        .FirstOrDefault();

                    string? fileUrl = null;
                    if (!string.IsNullOrWhiteSpace(ge.FilePath) &&
                        !ge.FilePath.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
                    {
                        fileUrl = ge.FilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? ge.FilePath
                            : $"{baseUrl}{ge.FilePath}";
                    }

                    resultRows.Add(new
                    {
                        id = ge.Id,
                        studentId = ogr.Id,
                        studentName = (ogr.Ad ?? "") + " " + (ogr.Soyad ?? ""),
                        className = ders?.DersAdi,
                        subject = ders?.DersAdi,
                        examTitle = ge.ExamTitle,
                        examDate = ge.UploadDate,
                        fileUrl,
                        feedback = summary,
                        topics = Array.Empty<string>(),
                        teacherName = teacher != null
                            ? ((teacher.Ad ?? "") + " " + (teacher.Soyad ?? "")).Trim()
                            : null
                    });
                }
            }

            return Ok(new { examResults = resultRows });
        }

        [HttpGet("guidance")]
        public async Task<IActionResult> GetGuidanceNotes()
        {
            var (userId, _, roles) = GetCurrentParent();

            if (string.IsNullOrWhiteSpace(userId) ||
                !roles.Any(r => r.Equals("Parent", StringComparison.OrdinalIgnoreCase)))
                return Ok(new { items = Array.Empty<object>() });

            var parent = await _db.Veliler
                .Include(v => v.Ogrenciler)
                .FirstOrDefaultAsync(v =>
                    v.AppUserId != null &&
                    v.AppUserId.ToLower() == userId.ToLower());

            if (parent == null)
                return Ok(new { items = Array.Empty<object>() });

            var studentIds = parent.Ogrenciler.Select(o => o.Id).ToList();
            if (!studentIds.Any())
                return Ok(new { items = Array.Empty<object>() });

            var studentIdSet = new HashSet<int>(studentIds);

            var notes = await _db.GuidanceNotes
                .AsNoTracking()
                .ToListAsync();

            var ogrenciler = await _db.Ogrenciler
                .AsNoTracking()
                .ToListAsync();

            var dersler = await _db.Dersler
                .AsNoTracking()
                .ToListAsync();

            var result = notes
                .Where(n => studentIdSet.Contains(n.StudentId))
                .Select(n =>
                {
                    var ogr = ogrenciler.FirstOrDefault(o => o.Id == n.StudentId);
                    var ders = dersler.FirstOrDefault(d => d.Id == n.ClassId);

                    return new
                    {
                        id = n.Id,
                        studentId = n.StudentId,
                        studentName = (ogr?.Ad ?? "") + " " + (ogr?.Soyad ?? ""),
                        classId = n.ClassId,
                        className = ders != null
                                  ? ders.DersAdi
                                 : (ogr?.Sinif != null ? ogr.Sinif.ToString() : "-"),

                        area = n.Area,
                        content = n.Content,
                        createdAt = n.CreatedAt,
                        createdBy = n.CreatedBy
                    };
                })
                .OrderByDescending(x => x.createdAt)
                .ToList();

            return Ok(new { items = result });
        }

    }
}
