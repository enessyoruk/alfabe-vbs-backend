#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Alfabe.Models.DTOs
{
    public class CreateHomeworkDto
    {
        [Required] public int classId { get; set; }
        [Required, StringLength(150)] public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        [Required] public string dueDate { get; set; } = string.Empty; // ISO/Date string
        public int? teacherId { get; set; }
    }

    public class UpdateHomeworkDto
    {
        [Required] public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        [Required] public string dueDate { get; set; } = string.Empty; // ISO/Date string
    }

    public class ExamResultUploadDto
    {
        [Required] public int studentId { get; set; }
        [Required] public int classId { get; set; }
        [Required] public string examDate { get; set; } = string.Empty; // ISO/Date string
        public string? score { get; set; } // "87.5" vb. (controller decimal parse ediyor)
        public int? teacherId { get; set; }
        public IFormFile? image { get; set; } // JPG/PNG
    }

    public class GeneralExamCreateDto
    {
       
        public List<int>? classIds { get; set; }

        [Required, StringLength(150)]
        public string examTitle { get; set; }

        public string? description { get; set; }

        public string? fileUrl { get; set; }

        public int? teacherId { get; set; }
    }

    public class GeneralExamAnalysisCreateDto
    {
        [Required] public int examId { get; set; }
        public string? summary { get; set; }
        public string? detailsJson { get; set; } // serbest JSON string
    }
}
