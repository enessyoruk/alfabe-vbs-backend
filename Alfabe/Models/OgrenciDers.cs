using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("OgrenciDersler")]  // Veritabanındaki tablo adı ile eşleştiriyoruz
    public class OgrenciDers
    {
        public int Id { get; set; }
        public int OgrenciId { get; set; }
        public int DersId { get; set; }
        public Ogrenci Ogrenci { get; set; }
        public Dersler Ders { get; set; }
    }
}
