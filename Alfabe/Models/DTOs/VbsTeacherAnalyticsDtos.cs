#nullable enable
using System;
using System.Collections.Generic;

namespace Alfabe.Models.DTOs
{
    public class VbsClassAnalyticsDto
    {
        public string classId { get; set; } = string.Empty;
        public string className { get; set; } = string.Empty;
        public int studentCount { get; set; }

        public decimal attendanceRate { get; set; }
        public decimal homeworkCompletionRate { get; set; }

        public List<VbsAnalyticsStudentDto> topPerformers { get; set; } = new();
        public List<VbsAnalyticsNeedsAttentionDto> needsAttention { get; set; } = new();
        public List<VbsClassInsightDto> classInsights { get; set; } = new();
        public List<VbsExamHistoryItemDto> examHistory { get; set; } = new();
    }

    public class VbsAnalyticsStudentDto
    {
        public string name { get; set; } = string.Empty;
        public decimal homeworkCompletion { get; set; }
        public decimal attendanceRate { get; set; }
        public decimal examParticipation { get; set; }
    }

    public class VbsAnalyticsNeedsAttentionDto
    {
        public string name { get; set; } = string.Empty;
        public decimal homeworkCompletion { get; set; }
        public decimal attendanceRate { get; set; }
        public decimal examParticipation { get; set; }
        public string reason { get; set; } = string.Empty;
    }

    public class VbsClassInsightDto
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string icon { get; set; } = string.Empty;   // "homework", "exam", "attendance", "trend"
        public string status { get; set; } = "normal";     // "excellent", "good", "warning", "normal"
    }

    public class VbsExamHistoryItemDto
    {
        public string id { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public DateTime date { get; set; }
        public int participation { get; set; }
        public int total { get; set; }
    }
}
