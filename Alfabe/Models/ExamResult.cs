using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("ExamResults")]
    public class ExamResult
    {
        public int Id { get; set; }

        [Required] public int OgrenciId { get; set; }
        [Required] public int DersId { get; set; }             // classId (Ders.Id)
        [Required] public DateTime ExamDate { get; set; }
        [Range(0, 100)] public decimal? Score { get; set; }    // opsiyonel: 0-100
        [StringLength(500)] public string? ImagePath { get; set; } // .jpg yolu (wwwroot altında)
        public int? UploadedByTeacherId { get; set; }              // opsiyonel
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Ogrenci? Ogrenci { get; set; }
        public Dersler? Ders { get; set; }
    }
}
