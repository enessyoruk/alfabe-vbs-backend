using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("Homework")]
    public class Homework
    {
        public int Id { get; set; }

        [Required] public int DersId { get; set; }            // classId olarak kullanacağız (Ders.Id)
        [Required, StringLength(150)] public string Title { get; set; } = "";
        [StringLength(2000)] public string? Description { get; set; }
        [Required] public DateTime DueDate { get; set; }       // teslim tarihi
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByTeacherId { get; set; }           // opsiyonel: Ogretmen.Id

        public Dersler? Ders { get; set; }
    }
}
