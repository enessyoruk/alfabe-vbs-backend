using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Alfabe.Models
{
    [Table("Adres")]
    public class Adres
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Adres bilgisi zorunludur.")]
        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string DetayAdres { get; set; } = string.Empty;

    }
}
