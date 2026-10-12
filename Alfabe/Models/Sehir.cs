namespace Alfabe.Models
{
    public class Sehir
    {
        public int Id { get; set; }
        public string SehirAdi { get; set; }

        public ICollection<Ilce> Ilceler { get; set; }
    }
}