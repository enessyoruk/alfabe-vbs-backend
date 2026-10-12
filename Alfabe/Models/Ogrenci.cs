using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Alfabe.Models
{
    [Table("Ogrenci")]
    public class Ogrenci
    {
        public Ogrenci()
        {
            Yoklamalar = new HashSet<Yoklama>();
            Odemeler = new HashSet<Odeme>();
            OgrenciDersler = new HashSet<OgrenciDers>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Ad 2 ile 30 karakter arasında olmalıdır.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Ad sadece harflerden oluşmalıdır.")]
        public string? Ad { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Soyad 2 ile 30 karakter arasında olmalıdır.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Soyad sadece harflerden oluşmalıdır.")]
        public string? Soyad { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? Telefon { get; set; }

        [Required(ErrorMessage = "Veli seçimi zorunludur.")]
        public int? VeliId { get; set; }

        [Required]
        [Range(5, 8, ErrorMessage = "Sınıf 5 ile 8 arasında olmalıdır.")]
        public int Sinif { get; set; }

        [ValidateNever]
        public Veli? Veli { get; set; }

        public bool AktifMi { get; set; } = true;

        [ValidateNever]
        public ICollection<Yoklama> Yoklamalar { get; set; }

        [ValidateNever]
        public ICollection<Odeme> Odemeler { get; set; }

        [ValidateNever]
        public ICollection<OgrenciDers> OgrenciDersler { get; set; }
    }
}
