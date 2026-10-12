using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("GeneralExams")]
    public class GeneralExam
    {
        public int Id { get; set; }

        [Required]
        public int DersId { get; set; }                    // classId (Dersler.Id)

        [Required, StringLength(150)]
        public string ExamTitle { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }              // /uploads/exams/general/xxx.jpg (opsiyonel)

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public int? TeacherId { get; set; }                // opsiyonel (oluşturan öğretmen)

        public int StudentCount { get; set; }              // sınıftaki öğrenci sayısı (opsiyonel kullanım)

        public Dersler? Ders { get; set; }

        public ICollection<GeneralExamAnalysis> Analyses { get; set; } = new List<GeneralExamAnalysis>();
    }
}
