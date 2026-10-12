using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models

{
    
    [Table("DevamDurumu")]  // Veritabanındaki gerçek tablo adı
    public class DevamDurumu

    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Devam durumu adı zorunludur.")]
        [StringLength(50, ErrorMessage = "En fazla 50 karakter olabilir.")]
        public string DurumAdi { get; set; }

        public ICollection<Yoklama> Yoklamalar { get; set; } = new List<Yoklama>();
    }

}
