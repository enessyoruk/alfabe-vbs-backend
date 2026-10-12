using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("GeneralExamAnalyses")]
    public class GeneralExamAnalysis
    {
        public int Id { get; set; }

        [Required] public int GeneralExamId { get; set; }
        [Required, StringLength(500)] public string Summary { get; set; } = default!;
        public string? DetailsJson { get; set; }        // serbest JSON
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByTeacherId { get; set; }

        public GeneralExam? GeneralExam { get; set; }
    }
}
