using Alfabe.Models;

public class DerslerOgretmen
{
    public int Id { get; set; }

    public int DersId { get; set; }
    public Dersler Ders { get; set; }

    public int OgretmenId { get; set; }
    public Ogretmen Ogretmen { get; set; }
}
