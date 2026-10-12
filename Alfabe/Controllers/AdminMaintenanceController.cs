using Alfabe.Data;
using Alfabe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminMaintenanceController : ControllerBase
    {
        private readonly AlfabeContext _db;

        public AdminMaintenanceController(AlfabeContext db)
        {
            _db = db;
        }

        [HttpPost("sync-student-lessons")]
        public async Task<IActionResult> SyncAllStudentLessons()
        {
            var students = await _db.Ogrenciler
                .Where(o => o.AktifMi)
                .ToListAsync();

            int total = students.Count;
            int processed = 0;

            foreach (var s in students)
            {
                await SyncLessonsForStudent(s.Id);
                processed++;
            }

            return Ok(new
            {
                message = "Tüm öğrencilerin dersleri güncellendi.",
                totalStudents = total,
                processed
            });
        }

        private async Task SyncLessonsForStudent(int studentId)
        {
            var ogrenci = await _db.Ogrenciler.FindAsync(studentId);
            if (ogrenci == null) return;

            var existing = await _db.OgrenciDersler
                .Where(od => od.OgrenciId == ogrenci.Id)
                .ToListAsync();

            _db.OgrenciDersler.RemoveRange(existing);

            var classLessons = await _db.Dersler
                .Where(d => d.SinifSeviyesi == ogrenci.Sinif)
                .ToListAsync();

            foreach (var d in classLessons)
            {
                await _db.OgrenciDersler.AddAsync(new OgrenciDers
                {
                    OgrenciId = ogrenci.Id,
                    DersId = d.Id
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}
