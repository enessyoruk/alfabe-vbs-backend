using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("Ilce")]  
    public class Ilce
    {
        public int Id { get; set; }
        public int SehirId { get; set; }
        public string Adi { get; set; }
        public Sehir Sehir { get; set; }
        public ICollection<Adres> Adresler { get; set; } = new List<Adres>();
    }
}
