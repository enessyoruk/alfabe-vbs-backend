using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Alfabe.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Ad Soyad sadece harflerden oluşmalıdır.")]
        public string AdSoyad { get; set; } = string.Empty;

        public string? SessionVersion { get; set; }
        public DateTime? SessionExpiresAt { get; set; }
    }
}
