using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Alfabe.Models
{
    [Table("Yoklama")]
    public class Yoklama
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tarih alanı zorunludur.")]
        [DataType(DataType.Date)]
        public DateTime Tarih { get; set; }

        [Required(ErrorMessage = "Öğrenci seçimi zorunludur.")]
        public int OgrenciId { get; set; }

        [ValidateNever]
        public virtual Ogrenci Ogrenci { get; set; }

        [Required(ErrorMessage = "Devam durumu seçimi zorunludur.")]
        public int DevamDurumuId { get; set; }

        [ValidateNever]
        public virtual DevamDurumu DevamDurumu { get; set; }
    }
}
