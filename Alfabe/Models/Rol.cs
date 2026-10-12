using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    [Table("Rol")]  
    public class Rol
    {
        public int Id { get; set; }
        public string RolAdi { get; set; }

        
    }
}
