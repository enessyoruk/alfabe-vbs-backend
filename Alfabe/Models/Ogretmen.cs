using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("Ogretmen")]
    public class Ogretmen
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Ad sadece harflerden oluşmalıdır.")]
        public string Ad { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Soyad sadece harflerden oluşmalıdır.")]
        public string Soyad { get; set; }

        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Telefon numarası 11 haneli olmalıdır.")]
        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string Telefon { get; set; }

        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
        public string? AppUserId { get; set; }

        [NotMapped]
        public ICollection<Dersler> Dersler { get; set; } = new List<Dersler>();
        public ICollection<DerslerOgretmen> DerslerOgretmen { get; set; } = new List<DerslerOgretmen>();

    }
}
