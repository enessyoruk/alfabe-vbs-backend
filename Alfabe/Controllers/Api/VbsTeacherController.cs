using Alfabe.Data;
using Alfabe.Models;
using Alfabe.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Alfabe.Controllers.Api
{
    [ApiController]
    [Route("api/vbs/teacher")]
    [Produces("application/json")]
    [Tags("VBS")]
    public class VbsTeacherController : ControllerBase
    {
        private readonly AlfabeContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public VbsTeacherController(
            AlfabeContext db,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _db = db;
            _env = env;
            _config = config;
        }

        
        private string GetSessionSecret()
        {
            var secret = _config["Jwt:SessionSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("SESSION_SECRET configuration is missing for VbsTeacherController.");
            }
            return secret;
        }

        private (string? userId, string? name) GetTeacherFromJwtCookie()
        {
            try
            {
                if (!Request.Cookies.TryGetValue("vbs_session", out var token) ||
                    string.IsNullOrWhiteSpace(token))
                {
                    return (null, null);
                }

                var secret = GetSessionSecret();
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };

                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwt = (JwtSecurityToken)validatedToken;

                var uid =
                    jwt.Claims.FirstOrDefault(c => c.Type == "uid")?.Value
                    ?? jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                return (uid, name);
            }
            catch
            {
                return (null, null);
            }
        }

        private (string? userId, string? fullName, IList<string> roles) GetCurrentTeacher()
        {
            try
            {
                if (!Request.Cookies.TryGetValue("vbs_session", out var token) ||
                    string.IsNullOrWhiteSpace(token))
                {
                    return (null, null, new List<string>());
                }

                var secret = GetSessionSecret();
                var key = Encoding.UTF8.GetBytes(secret);
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };

                tokenHandler.ValidateToken(token, validationParams, out var validated);

                var jwt = (JwtSecurityToken)validated;

                var uid = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                var fullName = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                var roleClaims = jwt.Claims.Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                                           .Select(c => c.Value)
                                           .ToList();

                var roles = new List<string>();

                foreach (var rc in roleClaims)
                {
                    if (string.IsNullOrWhiteSpace(rc)) continue;

                    if (rc.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            var parsed = System.Text.Json.JsonSerializer
                                .Deserialize<List<string>>(rc) ?? new List<string>();
                            roles.AddRange(parsed);
                        }
                        catch
                        {
                            roles.Add(rc);
                        }
                    }
                    else
                    {
                        roles.Add(rc);
                    }
                }

                return (uid, fullName, roles);
            }
            catch
            {
                return (null, null, new List<string>());
            }
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var (userId, _, roles) = GetCurrentTeacher();
            if (userId == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var teacher = await _db.Ogretmenler
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.AppUserId == userId);

            if (teacher == null)
                return Unauthorized(new { error = "Öğretmen kaydı bulunamadı." });

            var numericId = teacher.Id;

            var dersler = await _db.DerslerOgretmen
                .Where(x => x.OgretmenId == numericId)
                .Include(x => x.Ders)
                .ThenInclude(d => d.OgrenciDersler)
                .Select(x => new
                {
                    id = x.Ders.Id,
                    name = x.Ders.DersAdi,
                    subject = x.Ders.DersAdi,
                    grade = x.Ders.SinifSeviyesi,
                    studentCount = x.Ders.OgrenciDersler.Count(),
                    pendingHomework = 0,
                    recentExams = 0
                })
                .OrderBy(x => x.name)
                .ToListAsync();

            return Ok(new { items = dersler });
        }

        [HttpGet("classes/{classId:int}/students")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClassStudents([FromRoute] int classId)
        {
            var dersVarMi = await _db.Dersler.AnyAsync(d => d.Id == classId);
            if (!dersVarMi)
                return NotFound(new { error = "Ders bulunamadı." });

            var students = await _db.OgrenciDersler
                .Where(od => od.DersId == classId)
                .Select(od => new
                {
                    id = od.Ogrenci.Id,
                    firstName = od.Ogrenci.Ad,
                    lastName = od.Ogrenci.Soyad,
                    fullName = (od.Ogrenci.Ad ?? "") + " " + (od.Ogrenci.Soyad ?? ""),
                    phone = od.Ogrenci.Telefon,
                    grade = od.Ogrenci.Sinif,
                    isActive = od.Ogrenci.AktifMi
                })
                .OrderBy(s => s.fullName)
                .ToListAsync();

            return Ok(new { items = students, count = students.Count });
        }

        [HttpGet("homework")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHomeworks([FromQuery] int? classId = null)
        {
            var query = _db.Homeworks.AsQueryable();

            if (classId.HasValue)
                query = query.Where(h => h.DersId == classId.Value);

            var today = DateTime.UtcNow.Date;

            var items = await query
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    id = h.Id,
                    classId = h.DersId,
                    className = h.Ders.DersAdi,
                    title = h.Title,
                    description = h.Description,
                    assignedDate = h.CreatedAt,
                    dueDate = h.DueDate,
                    status = h.DueDate >= today ? "active" : "expired",
                    submissions = Array.Empty<object>(),
                    totalStudents = _db.OgrenciDersler.Count(od => od.DersId == h.DersId)
                })
                .ToListAsync();

            return Ok(new { items });
        }

        [HttpPost("homework")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateHomework([FromBody] CreateHomeworkDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz istek." });

            if (!DateTime.TryParse(dto.dueDate, out var due))
                return BadRequest(new { error = "Geçersiz teslim tarihi." });

            var ders = await _db.Dersler.FindAsync(dto.classId);
            if (ders == null)
                return NotFound(new { error = "Ders bulunamadı." });

            var hw = new Homework
            {
                DersId = dto.classId,
                Title = dto.title.Trim(),
                Description = dto.description?.Trim(),
                DueDate = due,
                CreatedAt = DateTime.UtcNow,
                CreatedByTeacherId = dto.teacherId
            };

            _db.Homeworks.Add(hw);
            await _db.SaveChangesAsync();

            return Created(string.Empty, new
            {
                id = hw.Id,
                classId = hw.DersId,
                title = hw.Title,
                description = hw.Description,
                dueDate = hw.DueDate,
                createdAt = hw.CreatedAt
            });
        }

        [HttpPut("homework")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateHomework([FromBody] UpdateHomeworkDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz istek." });

            if (!DateTime.TryParse(dto.dueDate, out var due))
                return BadRequest(new { error = "Geçersiz teslim tarihi." });

            var hw = await _db.Homeworks.FindAsync(dto.id);
            if (hw == null)
                return NotFound(new { error = "Ödev bulunamadı." });

            hw.Title = dto.title?.Trim() ?? hw.Title;
            hw.Description = dto.description?.Trim();
            hw.DueDate = due;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = hw.Id,
                classId = hw.DersId,
                title = hw.Title,
                description = hw.Description,
                dueDate = hw.DueDate
            });
        }

        [HttpDelete("homework")]
        public async Task<IActionResult> DeleteHomework([FromQuery] int? id = null, [FromQuery] string? action = null)
        {
            if (string.Equals(action, "cleanup", StringComparison.OrdinalIgnoreCase))
            {
                var cutoff = DateTime.UtcNow.Date.AddDays(-7);

                var oldExpired = await _db.Homeworks
                    .Where(h => h.DueDate < cutoff)
                    .ToListAsync();

                _db.Homeworks.RemoveRange(oldExpired);

                var n = await _db.SaveChangesAsync();

                return Ok(new { success = true, message = $"{n} eski ödev temizlendi" });
            }

            if (!id.HasValue)
                return BadRequest(new { error = "id zorunlu." });

            var hw = await _db.Homeworks.FindAsync(id.Value);
            if (hw == null)
                return NotFound(new { error = "Ödev bulunamadı." });

            _db.Homeworks.Remove(hw);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Ödev silindi." });
        }

        [HttpGet("dashboard-analytics")]
        public async Task<IActionResult> GetAnalytics(
    [FromQuery] int? teacherId = null,
    [FromQuery] int? classId = null)
        {
            var today = DateTime.UtcNow.Date;
            var attendanceFromDate = today.AddDays(-30);
            var examFromDate = today.AddDays(-60);

            if (classId.HasValue && classId.Value > 0)
            {
                var ders = await _db.Dersler
                    .Include(d => d.OgrenciDersler)
                        .ThenInclude(od => od.Ogrenci)
                    .FirstOrDefaultAsync(d => d.Id == classId.Value);

                if (ders == null)
                    return NotFound(new { error = "Ders bulunamadı." });

                var studentLinks = ders.OgrenciDersler.ToList();
                var studentIds = studentLinks.Select(x => x.OgrenciId).Distinct().ToList();

                if (studentIds.Count == 0)
                {
                    return Ok(new
                    {
                        classId = ders.Id,
                        className = ders.DersAdi,
                        studentCount = 0,
                        attendanceRate = 0,
                        homeworkCompletionRate = 0,
                        topPerformers = Array.Empty<object>(),
                        needsAttention = Array.Empty<object>(),
                        classInsights = new[]
                        {
                    new {
                        title = "Devam durumu",
                        description = "Bu sınıfa henüz öğrenci atanmadığı için analiz üretilemedi.",
                        icon = "attendance",
                        status = "warning"
                    }
                },
                        examHistory = Array.Empty<object>()
                    });
                }

                var attendanceAll = await _db.Yoklamalar
                    .Where(y => y.Tarih >= attendanceFromDate)
                    .ToListAsync();

                var attendanceRaw = attendanceAll
                    .Where(y => studentIds.Contains(y.OgrenciId))
                    .ToList();

                var attendanceByStudent = attendanceRaw
                    .GroupBy(y => y.OgrenciId)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            var relevant = g.Where(x => x.DevamDurumuId == 1 || x.DevamDurumuId == 2).ToList();
                            return new
                            {
                                Total = relevant.Count,
                                Present = relevant.Count(x => x.DevamDurumuId == 1)
                            };
                        });

                var examResults = await _db.ExamResults
                    .Where(e => e.DersId == ders.Id && e.ExamDate >= examFromDate)
                    .ToListAsync();

                var classExamDates = examResults.Select(e => e.ExamDate.Date).Distinct().ToList();
                int classExamCount = classExamDates.Count;

                var examByStudent = examResults
                    .GroupBy(e => e.OgrenciId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ExamDate.Date).Distinct().Count()
                    );

                static string BuildRiskReason(int attRate, double examScore, int examCount)
                {
                    if (attRate < 60 && examCount > 0 && examScore < 40)
                        return $"Devam oranı çok düşük (%{attRate}) ve sınav katılımı zayıf.";
                    if (attRate < 60)
                        return $"Devam oranı çok düşük (%{attRate}).";
                    if (attRate < 80)
                    {
                        if (examCount > 0 && examScore < 40)
                            return $"Devam oranı düşük (%{attRate}) ve sınav katılımı düşük.";
                        return $"Devam oranı düşük (%{attRate}).";
                    }
                    if (examCount > 0 && examScore < 40)
                        return $"Sınavlara katılım düşük (yaklaşık %{examScore:0}).";

                    return "Dikkat gerektiren durum.";
                }

                var perStudent = studentLinks
                    .Select(sl =>
                    {
                        var s = sl.Ogrenci;
                        if (s == null) return null;

                        int sid = s.Id;
                        int attendanceRate = 0;
                        int attendanceTotal = 0;

                        if (attendanceByStudent.TryGetValue(sid, out var att) && att.Total > 0)
                        {
                            attendanceTotal = att.Total;
                            attendanceRate = (int)Math.Round(100.0 * att.Present / att.Total);
                        }

                        double examScore = 0.0;
                        if (classExamCount > 0 &&
                            examByStudent.TryGetValue(sid, out var stuExamCount) &&
                            stuExamCount > 0)
                        {
                            examScore = Math.Clamp(
                                ((double)stuExamCount / classExamCount) * 100.0,
                                0.0,
                                100.0
                            );
                        }

                        var engagement = 0.85 * attendanceRate + 0.15 * examScore;

                        double riskScore = 0.0;
                        if (attendanceRate < 80) riskScore += 0.7;
                        if (attendanceRate < 60) riskScore += 0.2;
                        if (classExamCount > 0 && examScore < 40) riskScore += 0.1;

                        bool isAtRisk = riskScore >= 0.5;

                        var name = $"{s.Ad ?? ""} {s.Soyad ?? ""}".Trim();
                        if (string.IsNullOrWhiteSpace(name)) name = $"Öğrenci #{sid}";

                        return new
                        {
                            StudentId = sid,
                            Name = name,
                            AttendanceRate = attendanceRate,
                            AttendanceTotal = attendanceTotal,
                            ExamParticipationScore = examScore,
                            EngagementScore = engagement,
                            RiskScore = riskScore,
                            IsAtRisk = isAtRisk
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                int totalStudents = perStudent.Count;

                double avgAttendance = totalStudents > 0
                    ? perStudent.Average(s => (double)s.AttendanceRate)
                    : 0.0;

                double avgExamParticipation = (classExamCount > 0 && totalStudents > 0)
                    ? perStudent.Average(s => s.ExamParticipationScore)
                    : 0.0;

                var atRiskStudents = perStudent.Where(s => s.IsAtRisk).ToList();
                double riskRatio = totalStudents > 0
                    ? (double)atRiskStudents.Count / totalStudents
                    : 0.0;

                var topPerformers = perStudent
                    .Where(s => s.AttendanceTotal > 0)
                    .OrderByDescending(s => s.EngagementScore)
                    .Take(5)
                    .Select(s => new
                    {
                        name = s.Name,
                        homeworkCompletion = Math.Round(s.EngagementScore),
                        attendanceRate = s.AttendanceRate,
                        examParticipation = Math.Round(s.ExamParticipationScore, 1)
                    })
                    .ToList();

                var needsAttention = atRiskStudents
                    .OrderByDescending(s => s.RiskScore)
                    .Take(5)
                    .Select(s => new
                    {
                        name = s.Name,
                        homeworkCompletion = s.AttendanceRate,
                        attendanceRate = s.AttendanceRate,
                        examParticipation = Math.Round(s.ExamParticipationScore, 1),
                        reason = BuildRiskReason(
                            s.AttendanceRate,
                            s.ExamParticipationScore,
                            classExamCount)
                    })
                    .ToList();

                var classInsights = new List<object>();

                string attendanceStatus =
                    avgAttendance >= 90 ? "excellent" :
                    avgAttendance >= 80 ? "good" : "warning";

                classInsights.Add(new
                {
                    title = "Devam durumu",
                    description =
                        attendanceStatus == "excellent"
                            ? "Genel devam oranı oldukça yüksek."
                        : attendanceStatus == "good"
                            ? "Devam oranı genel olarak iyi, bazı öğrencilerde düşüşler var."
                        : "Devam oranı düşük, yoklama takibini sıkılaştırmak faydalı olabilir.",
                    icon = "attendance",
                    status = attendanceStatus
                });

                string riskStatus =
                    riskRatio >= 0.25 ? "warning" :
                    riskRatio >= 0.10 ? "good" : "excellent";

                classInsights.Add(new
                {
                    title = "Risk profili",
                    description =
                        riskStatus == "warning"
                            ? "Öğrencilerin önemli bir kısmı risk grubunda."
                        : riskStatus == "good"
                            ? "Az sayıda öğrenci risk grubunda, onları yakından takip etmek faydalı olacaktır."
                        : "Risk grubunda çok az öğrenci var. Genel durum stabil görünüyor.",
                    icon = "trend",
                    status = riskStatus
                });

                if (classExamCount > 0)
                {
                    string examStatus =
                        avgExamParticipation >= 70 ? "good" :
                        avgExamParticipation >= 40 ? "warning" : "warning";

                    classInsights.Add(new
                    {
                        title = "Sınav katılımı",
                        description =
                            examStatus == "good"
                                ? "Tanımlanan sınavlara katılım genel olarak iyi seviyede."
                                : "Sınavlara katılım düşük, öğrencilerin sınav takibinin güçlendirilmesi önerilir.",
                        icon = "exam",
                        status = examStatus
                    });
                }

                classInsights.Add(new
                {
                    title = "Ödev durumu",
                    description = "Ödev tamamlama verileri yakında bu alanda gösterilecek.",
                    icon = "homework",
                    status = "normal"
                });

                var examHistory = await _db.GeneralExams
                    .Where(ge => ge.DersId == ders.Id)
                    .OrderByDescending(ge => ge.UploadDate)
                    .Take(10)
                    .Select(ge => new
                    {
                        id = ge.Id,
                        title = ge.ExamTitle ?? "Sınav",
                        date = ge.UploadDate,
                        participation = (int?)null,
                        total = ge.StudentCount > 0 ? (int?)ge.StudentCount : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    classId = ders.Id,
                    className = ders.DersAdi,
                    studentCount = totalStudents,
                    attendanceRate = Math.Round(avgAttendance, 1),
                    homeworkCompletionRate = 0.0,
                    topPerformers,
                    needsAttention,
                    classInsights,
                    examHistory
                });
            }

            var examCutoffGlobal = today.AddDays(-30);

            var dersListRaw = await _db.Dersler
                .Include(d => d.OgrenciDersler)
                .ToListAsync();

            var dersList = dersListRaw
                .Select(d => new
                {
                    d.Id,
                    d.DersAdi,
                    studentCount = d.OgrenciDersler.Count
                })
                .ToList();

            var classIds = dersList.Select(d => d.Id).ToList();

            int activeHomework = 0;
            int pendingExams = 0;

            if (classIds.Count > 0)
            {
                var allHomeworks = await _db.Homeworks
                    .Where(h => h.DueDate >= today)
                    .ToListAsync();

                activeHomework = allHomeworks.Count(h => classIds.Contains(h.DersId));

                var allExamResults = await _db.ExamResults
                    .Where(e => e.ExamDate >= examCutoffGlobal)
                    .ToListAsync();

                pendingExams = allExamResults.Count(e => classIds.Contains(e.DersId));
            }

            var classPerformance = new List<object>();

            foreach (var d in dersList)
            {
                var studentIds = await _db.OgrenciDersler
                    .Where(od => od.DersId == d.Id)
                    .Select(od => od.OgrenciId)
                    .ToListAsync();

                if (studentIds.Count == 0)
                {
                    classPerformance.Add(new
                    {
                        className = d.DersAdi,
                        homeworkParticipation = 0,
                        attendanceAverage = 0.0,
                        trend = "down"
                    });
                    continue;
                }

                var attendanceAll = await _db.Yoklamalar
                    .Where(y => y.Tarih >= attendanceFromDate)
                    .ToListAsync();

                var attendance = attendanceAll
                    .Where(y => studentIds.Contains(y.OgrenciId))
                    .ToList();

                var grouped = attendance
                    .GroupBy(y => y.OgrenciId)
                    .Select(g =>
                    {
                        var relevant = g.Where(x => x.DevamDurumuId == 1 || x.DevamDurumuId == 2).ToList();
                        if (relevant.Count == 0) return 0.0;

                        double val = (double)relevant.Count(x => x.DevamDurumuId == 1)
                                     / relevant.Count * 100.0;
                        return val;
                    })
                    .ToList();

                double avg =
                    grouped.Count > 0 ? grouped.Average() : 0.0;

                classPerformance.Add(new
                {
                    className = d.DersAdi,
                    homeworkParticipation = 0,
                    attendanceAverage = Math.Round(avg, 1),
                    trend = avg >= 80.0 ? "up" : "down"
                });
            }

            return Ok(new
            {
                activeHomework,
                pendingExams,
                classPerformance
            });
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalyticsAlias([FromQuery] int? classId = null, [FromQuery] int? teacherId = null)
        {
            return await GetAnalytics(teacherId, classId);
        }

        [HttpGet("guidance")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGuidance([FromQuery] int? classId = null)
        {
            var now = DateTime.UtcNow;

            IQueryable<Ogrenci> studentsQuery = _db.Ogrenciler.AsNoTracking();

            if (classId.HasValue && classId.Value > 0)
            {
                studentsQuery =
                    from o in _db.Ogrenciler.AsNoTracking()
                    join od in _db.OgrenciDersler.AsNoTracking()
                        on o.Id equals od.OgrenciId
                    where od.DersId == classId.Value
                    select o;
            }

            var students = await studentsQuery.ToListAsync();
            var studentIds = students.Select(s => s.Id).ToList();

            if (studentIds.Count == 0)
            {
                return Ok(new
                {
                    summary = new { totalStudents = 0, atRiskStudents = 0 },
                    items = Array.Empty<object>()
                });
            }

            var fromDate = DateTime.UtcNow.Date.AddDays(-30);

            var attendanceRaw = await _db.Yoklamalar
                .Where(y => y.Tarih >= fromDate)
                .ToListAsync();

            attendanceRaw = attendanceRaw
                .Where(y => studentIds.Contains(y.OgrenciId))
                .ToList();

            var attendanceLookup = attendanceRaw
                .GroupBy(y => y.OgrenciId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var relevant = g
                            .Where(x => x.DevamDurumuId == 1 || x.DevamDurumuId == 2)
                            .ToList();

                        var total = relevant.Count;
                        var present = relevant.Count(x => x.DevamDurumuId == 1);

                        return new { Total = total, Present = present };
                    });

            var allNotes = await _db.GuidanceNotes
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notesRaw = allNotes
                .Where(n => studentIds.Contains(n.StudentId))
                .ToList();

            var notesLookup = notesRaw
                .GroupBy(n => n.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = students.Select(o =>
            {
                int attendanceRate = 0;

                if (attendanceLookup.TryGetValue(o.Id, out var att) && att.Total > 0)
                {
                    attendanceRate =
                        (int)Math.Round(100.0 * att.Present / (double)att.Total);
                }

                var atRisk = attendanceRate < 85;

                var fullName = (o.Ad ?? "") + " " + (o.Soyad ?? "");

                int sinifDeger = 0;
                if (int.TryParse(o.Sinif.ToString(), out var sVal))
                    sinifDeger = sVal;

                var classIdValue = sinifDeger > 0 ? sinifDeger : 0;
                var classNameValue =
                    sinifDeger > 0 ? $"{sinifDeger}. Sınıf" : "Sınıf bilgisi yok";

                var guidanceNotes = new List<object>();
                if (notesLookup.TryGetValue(o.Id, out var notesForStudent))
                {
                    guidanceNotes = notesForStudent
                        .Select(n => (object)new
                        {
                            id = n.Id,
                            studentId = n.StudentId,
                            classId = n.ClassId,
                            area = n.Area,
                            content = n.Content,
                            isForParent = n.IsForParent,
                            createdAt = n.CreatedAt,
                            createdBy = n.CreatedBy,
                            createdById = n.CreatedById
                        })
                        .ToList();
                }

                return new
                {
                    studentId = o.Id,
                    id = o.Id,
                    name = fullName,
                    studentName = fullName,
                    classId = classIdValue,
                    className = classNameValue,
                    homeworkParticipation = 50,
                    examParticipation = 50,
                    attendanceRate,
                    emotionalState = atRisk ? "at-risk" : "stable",
                    needsAttention = atRisk,
                    isAtRisk = atRisk,
                    status = atRisk ? "at-risk" : "ok",
                    riskScore = atRisk ? 0.8 : 0.2,
                    riskLevel = atRisk ? "high" : "low",
                    lastUpdate = now,
                    notes = atRisk
                        ? "Bu öğrenci rehberlik açısından yakından izlenmeli."
                        : "Genel durumu stabil görünüyor.",
                    guidanceNotes
                };
            }).ToList();

            var total = items.Count;
            var atRiskCount = items.Count(x => x.needsAttention || x.isAtRisk);

            return Ok(new
            {
                summary = new { totalStudents = total, atRiskStudents = atRiskCount },
                items
            });
        }

        [HttpPost("guidance")]
        [Consumes("application/json")]
        public async Task<IActionResult> AddGuidanceNote([FromBody] TeacherGuidanceNoteCreateDto dto)
        {
            

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _db.Ogrenciler.FindAsync(dto.StudentId);
            if (student == null)
                return NotFound(new { success = false, message = "Öğrenci bulunamadı." });

            int? classId = null;

            if (dto.ClassId.HasValue)
            {
                classId = dto.ClassId.Value;

                var dersVarMi = await _db.Dersler
                    .AnyAsync(d => d.SinifSeviyesi == classId.Value);

                if (!dersVarMi)
                    classId = null;
            }

            var (teacherUserId, teacherName) = GetTeacherFromJwtCookie();

            var note = new GuidanceNote
            {
                StudentId = dto.StudentId,
                ClassId = classId,
                Area = dto.Area?.Trim() ?? "",
                Content = dto.Content?.Trim() ?? "",
                CreatedAt = DateTime.UtcNow,
                IsForParent = false,
                CreatedById = string.IsNullOrWhiteSpace(teacherUserId) ? null : teacherUserId,
                CreatedBy = string.IsNullOrWhiteSpace(teacherName) ? null : teacherName
            };

            _db.GuidanceNotes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                id = note.Id,
                message = "Rehberlik notu kaydedildi.",
                teacherUserId,
                teacherName
            });
        }

        [HttpPost("guidance/parent-note")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateParentGuidanceNote([FromBody] ParentGuidanceNoteCreateDto dto)
        {
            

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _db.Ogrenciler.FindAsync(dto.StudentId);
            if (student == null)
                return NotFound(new { success = false, message = "Öğrenci bulunamadı." });

            var (teacherUserId, teacherName) = GetTeacherFromJwtCookie();

            var note = new GuidanceNote
            {
                StudentId = dto.StudentId,
                ClassId = null,
                Area = "Veli Notu",
                Content = dto.Content?.Trim() ?? "",
                IsForParent = true,
                CreatedAt = DateTime.UtcNow,
                CreatedById = string.IsNullOrWhiteSpace(teacherUserId) ? null : teacherUserId,
                CreatedBy = string.IsNullOrWhiteSpace(teacherName) ? null : teacherName
            };

            _db.GuidanceNotes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = note.Id, message = "Veliye yönelik rehberlik notu kaydedildi." });
        }

        [HttpGet("total-students")]
        public async Task<IActionResult> GetTotalStudents()
        {
            var (userId, _, _) = GetCurrentTeacher();
            if (userId == null)
                return Unauthorized(new { error = "JWT okunamadı" });

            var teacher = await _db.Ogretmenler
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.AppUserId == userId);

            if (teacher == null)
                return Unauthorized(new { error = "Öğretmen kaydı bulunamadı" });

            var numericId = teacher.Id;

            var dersIds = await _db.DerslerOgretmen
                .Where(x => x.OgretmenId == numericId)
                .Select(x => x.DersId)
                .ToListAsync();

            if (dersIds.Count == 0)
                return Ok(new { totalStudents = 0 });

            var allLinks = await _db.OgrenciDersler
                .Where(od => od.DersId > 0) // dumb filter (always true)
                .Select(od => new { od.DersId, od.OgrenciId })
                .ToListAsync();

            var totalStudents = allLinks
                .Where(od => dersIds.Contains(od.DersId))
                .Select(od => od.OgrenciId)
                .Distinct()
                .Count();

            return Ok(new { totalStudents });
        }

    }
}
