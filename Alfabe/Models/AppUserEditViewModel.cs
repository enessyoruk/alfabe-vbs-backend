using System.ComponentModel.DataAnnotations;

namespace Alfabe.Models.ViewModels
{
    public class AppUserEditViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [RegularExpression(@"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$", ErrorMessage = "Ad Soyad sadece harflerden oluşmalıdır.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; }

        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Yeni şifre alanı zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [RegularExpression(@"^(?=.*[A-Z])[a-zA-Z0-9]*$", ErrorMessage = "Şifre en az bir büyük harf içermeli ve sadece harf ve rakam içerebilir.")]
        public string? NewPassword { get; set; }


        [Display(Name = "Yeni Şifre Tekrar")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string? ConfirmPassword { get; set; }
    }
}
