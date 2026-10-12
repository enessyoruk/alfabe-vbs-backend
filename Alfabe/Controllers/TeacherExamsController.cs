using Alfabe.Data;
using Alfabe.Models;
using Alfabe.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Alfabe.Controllers
{
    [ApiController]
    [Route("api/vbs/teacher/exams")]
    public class TeacherExamsController : ControllerBase
    {
        private readonly AlfabeContext _db;
        private readonly IWebHostEnvironment _env;

        public TeacherExamsController(AlfabeContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private string? GetUidFromCookie()
        {
            if (!Request.Cookies.TryGetValue("vbs_session", out var token) ||
                string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwt = handler.ReadJwtToken(token);

                var uid =
                    jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ??
                    jwt.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

                return string.IsNullOrWhiteSpace(uid) ? null : uid;
            }
            catch
            {
                return null;
            }
        }

        private async Task<int?> ResolveTeacherId()
        {
            var uid = GetUidFromCookie();
            if (uid == null) return null;

            var teacher = await _db.Ogretmenler
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.AppUserId == uid);

            return teacher?.Id;
        }

        private string UploadRoot =>
            Path.Combine(_env.ContentRootPath, "uploads");

        [HttpGet("general")]
        public async Task<IActionResult> GetExams()
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var exams = await _db.GeneralExams
                .Include(x => x.Ders)
                .Include(x => x.Analyses)
                .OrderByDescending(x => x.UploadDate)
                .Select(x => new
                {
                    id = x.Id,
                    examTitle = x.ExamTitle,
                    classId = x.DersId,
                    className = x.Ders != null ? x.Ders.DersAdi : null,
                    uploadDate = x.UploadDate,

                    fileUrl = string.IsNullOrWhiteSpace(x.FilePath)
                        ? null
                        : $"{Request.Scheme}://{Request.Host}{x.FilePath}",

                    studentCount = _db.OgrenciDersler.Count(od => od.DersId == x.DersId),
                    hasAnalysis = x.Analyses.Any(),
                    analysis = x.Analyses
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.Summary)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(exams);
        }

        [HttpGet]
        public Task<IActionResult> GetExamsAlias() => GetExams();

        [HttpPost("general")]
        public async Task<IActionResult> Create([FromBody] GeneralExamCreateDto dto)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            if (!ModelState.IsValid)
                return BadRequest(new { error = "Geçersiz istek." });

            if (dto.classIds == null || dto.classIds.Count == 0)
                return BadRequest(new { error = "classIds gerekli." });

            var classIds = dto.classIds.Where(x => x > 0).Distinct().ToList();
            var output = new List<object>();

            foreach (var cid in classIds)
            {
                var ders = await _db.Dersler.FindAsync(cid);
                if (ders == null) continue;

                var studentCount = await _db.OgrenciDersler.CountAsync(x => x.DersId == cid);

                var exam = new GeneralExam
                {
                    DersId = cid,
                    ExamTitle = dto.examTitle.Trim(),
                    Description = dto.description?.Trim(),
                    FilePath = dto.fileUrl,
                    UploadDate = DateTime.UtcNow,
                    TeacherId = tid.Value,
                    StudentCount = studentCount
                };

                _db.GeneralExams.Add(exam);
                await _db.SaveChangesAsync();

                output.Add(new
                {
                    id = exam.Id,
                    classId = exam.DersId,
                    className = ders.DersAdi,
                    examTitle = exam.ExamTitle,
                    description = exam.Description,
                    uploadDate = exam.UploadDate,
                    fileUrl = exam.FilePath,
                    studentCount = exam.StudentCount
                });
            }

            return Ok(output);
        }
        [HttpPost]
        public Task<IActionResult> CreateAlias([FromBody] GeneralExamCreateDto dto)
        {
            return Create(dto);
        }

        [HttpPost("upload/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadGeneralExamImage([FromForm] UploadGeneralExamImageDto dto)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var image = dto.Image;
            if (image == null || image.Length == 0)
                return BadRequest(new { error = "Resim yok." });

            if (!(image.ContentType is "image/jpeg" or "image/jpg" or "image/png"))
                return BadRequest(new { error = "Yalnızca JPG/PNG." });

            var dir = Path.Combine(UploadRoot, "general-exams");
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName)}";
            var full = Path.Combine(dir, fileName);

            using (var fs = System.IO.File.Create(full))
                await image.CopyToAsync(fs);

            var relative = $"/uploads/general-exams/{fileName}";

            return Ok(new { fileUrl = relative });
        }

        [HttpPost("exam-results")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExamResult([FromForm] ExamResultUploadDto dto)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            if (!await _db.Ogrenciler.AnyAsync(o => o.Id == dto.studentId))
                return NotFound(new { error = "Öğrenci yok." });

            if (!await _db.Dersler.AnyAsync(d => d.Id == dto.classId))
                return NotFound(new { error = "Ders yok." });

            if (!DateTime.TryParse(dto.examDate, out var examDt))
                return BadRequest(new { error = "Geçersiz tarih." });

            string? saved = null;

            if (dto.image != null && dto.image.Length > 0)
            {
                if (!(dto.image.ContentType is "image/jpeg" or "image/jpg" or "image/png"))
                    return BadRequest(new { error = "Yalnızca JPG/PNG." });

                var dir = Path.Combine(UploadRoot, "exams");
                Directory.CreateDirectory(dir);

                var file = $"{Guid.NewGuid():N}{Path.GetExtension(dto.image.FileName)}";
                var full = Path.Combine(dir, file);

                using (var fs = System.IO.File.Create(full))
                    await dto.image.CopyToAsync(fs);

                saved = $"/uploads/exams/{file}";
            }

            var ex = new ExamResult
            {
                OgrenciId = dto.studentId,
                DersId = dto.classId,
                ExamDate = examDt.Date,
                Score = dto.score != null ? decimal.Parse(dto.score) : null,
                ImagePath = saved,
                UploadedByTeacherId = tid,
                CreatedAt = DateTime.UtcNow
            };

            _db.ExamResults.Add(ex);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = ex.Id,
                examDate = ex.ExamDate,
                score = ex.Score,
                imageUrl = ex.ImagePath
            });
        }

        [HttpGet("{id}/analysis")]
        public async Task<IActionResult> GetAnalyses(int id)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var exam = await _db.GeneralExams.FindAsync(id);
            if (exam == null)
                return NotFound(new { error = "Sınav yok." });

            var list = await _db.GeneralExamAnalyses
                .Where(a => a.GeneralExamId == id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("analysis")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateGeneralExamAnalysis([FromBody] GeneralExamAnalysisCreateDto dto)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var exam = await _db.GeneralExams.FindAsync(dto.examId);
            if (exam == null)
                return NotFound(new { error = "Sınav bulunamadı." });

            var analysis = new GeneralExamAnalysis
            {
                GeneralExamId = dto.examId,
                Summary = dto.summary?.Trim(),
                DetailsJson = dto.detailsJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.GeneralExamAnalyses.Add(analysis);
            await _db.SaveChangesAsync();

            return Ok(analysis);
        }

        [HttpDelete("general/delete")]
        public async Task<IActionResult> DeleteGeneralExam([FromQuery] int id)
        {
            var tid = await ResolveTeacherId();
            if (tid == null)
                return Unauthorized(new { error = "JWT okunamadı." });

            var exam = await _db.GeneralExams.FindAsync(id);
            if (exam == null)
                return NotFound(new { error = "Sınav bulunamadı." });

            var analyses = _db.GeneralExamAnalyses.Where(a => a.GeneralExamId == id);
            _db.GeneralExamAnalyses.RemoveRange(analyses);
            _db.GeneralExams.Remove(exam);

            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("general/delete")]
        public Task<IActionResult> DeleteGeneralExamPost([FromQuery] int id)
        {
            return DeleteGeneralExam(id);
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest(new { error = "path required" });

            path = path.TrimStart('/');

            var full = Path.Combine(UploadRoot, path.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(full))
                return NotFound(new { error = "file not found", full });

            var fileName = Path.GetFileName(full);
            var bytes = System.IO.File.ReadAllBytes(full);

            Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";

            return File(bytes, "application/octet-stream", fileName);
        }
    }
}
