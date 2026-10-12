using System;

namespace Alfabe.Models
{
    public class GuidanceNote
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int? ClassId { get; set; }

        public string? Area { get; set; }

        public string Content { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public string? CreatedById { get; set; }

        public string? CreatedBy { get; set; }

        public bool IsForParent { get; set; }
    }
}
