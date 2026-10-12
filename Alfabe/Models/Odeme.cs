using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("Odeme")]
    public class Odeme
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ödeme Tarihi zorunludur.")]
        public DateTime OdemeTarihi { get; set; }

        [Required(ErrorMessage = "Tutar zorunludur.")]
        [Range(0.01, 9999999999999999.99, ErrorMessage = "Tutar çok büyük veya 0'dan küçük olamaz.")]
        public decimal Tutar { get; set; }

        public bool OdendiMi { get; set; }

        [Required(ErrorMessage = "Öğrenci seçimi zorunludur.")]
        public int OgrenciId { get; set; }

        public Ogrenci? Ogrenci { get; set; }


        public string OdemeTipi { get; set; }

        public string? Aciklama { get; set; }
    }
}
