using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Alfabe.Models
{
    [Table("Veli")]
    public class Veli
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public required string Ad { get; set; }

        [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public required string Soyad { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        [StringLength(11, ErrorMessage = "Telefon numarası en fazla 11 karakter olabilir.")]
        public string? Telefon { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
        [StringLength(11, ErrorMessage = "Telefon numarası en fazla 11 karakter olabilir.")]
        public string? Telefon2 { get; set; }

        [ValidateNever]
        public int? AdresId { get; set; }
        public Adres? Adres { get; set; }

        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
        

        public ICollection<Ogrenci> Ogrenciler { get; set; } = new List<Ogrenci>();
    }
}
