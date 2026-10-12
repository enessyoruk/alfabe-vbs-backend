using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alfabe.Models.ViewModels
{
    public class YoklamaViewModel
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }

        public int OgrenciId { get; set; }

        public int DevamDurumuId { get; set; }
        public int Sinif { get; set; }

        public List<SelectListItem> OgrencilerListesi { get; set; } = new();
        public List<SelectListItem> DevamDurumlariListesi { get; set; } = new();

        public List<YoklamaOgrenciSecim> OgrenciSecimleri { get; set; } = new();
    }

    public class YoklamaOgrenciSecim
    {
        public int OgrenciId { get; set; }
        public int DevamDurumuId { get; set; }
    }
}
