namespace Alfabe.Models
{
    public class SmsLog
    {
        public int Id { get; set; }
        public string Telefon { get; set; }
        public string Mesaj { get; set; }
        public DateTime GonderimTarihi { get; set; }
        public bool BasariliMi { get; set; }  // Başarılı mı değil mi
        public string Durum { get; set; }      // Detaylı mesaj (başarılı veya hata mesajı)
    }
}
