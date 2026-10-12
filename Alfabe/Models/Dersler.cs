using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    public class Dersler
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ders adı gereklidir.")]
        [StringLength(100, ErrorMessage = "Ders adı en fazla 100 karakter olabilir.")]
        public string DersAdi { get; set; }

        [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
        public string Aciklama { get; set; }

        [Display(Name = "Sınıf Seviyesi")]
        [Range(1, 12, ErrorMessage = "Sınıf seviyesi 1 ile 12 arasında olmalıdır.")]
        public int? SinifSeviyesi { get; set; }

        public ICollection<OgrenciDers> OgrenciDersler { get; set; } = new List<OgrenciDers>();

        [NotMapped]
        public ICollection<Ogretmen> Ogretmenler { get; set; } = new List<Ogretmen>();
        public ICollection<DerslerOgretmen> DerslerOgretmen { get; set; } = new List<DerslerOgretmen>();

    }
}
