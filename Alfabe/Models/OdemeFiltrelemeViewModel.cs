using System;
using System.Collections.Generic;
using Alfabe.Models.ViewModels;

namespace Alfabe.Models
{
    public class OdemeFiltrelemeViewModel
    {
        public string? OgrenciAdi { get; set; }
        public string? OdemeDurumu { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }

        public List<Odeme> Odemeler { get; set; } = new List<Odeme>();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
