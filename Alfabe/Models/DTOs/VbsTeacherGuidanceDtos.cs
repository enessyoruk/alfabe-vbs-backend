#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Alfabe.Models.DTOs
{
    public class GuidanceNoteItemDto
    {
        public string Area { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        public string? CreatedBy { get; set; }
    }

    public class GuidanceStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;

        public int HomeworkParticipation { get; set; }  // %
        public int ExamParticipation { get; set; }      // %
        public int AttendanceRate { get; set; }         // %

        public string EmotionalState { get; set; } = "stable";
        public bool NeedsAttention { get; set; }

        public DateTime LastUpdate { get; set; }
        public string? Notes { get; set; }

        public List<GuidanceNoteItemDto> GuidanceNotes { get; set; } = new();
    }

    public class TeacherGuidanceListResponseDto
    {
        public bool Success { get; set; } = true;
        public List<GuidanceStudentDto> Students { get; set; } = new();
    }

    public class TeacherGuidanceNoteCreateDto
    {
        [Required]
        public int StudentId { get; set; }

        public int? ClassId { get; set; }

        [Required, StringLength(100)]
        public string Area { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Content { get; set; } = string.Empty;
    }

    public class ParentGuidanceNoteCreateDto
    {
        public int StudentId { get; set; }
        public string Content { get; set; } = "";
    }
}
