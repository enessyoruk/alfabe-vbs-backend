#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Alfabe.Models.DTOs
{
    public class UploadGeneralExamImageDto
    {
   
        [FromForm(Name = "image")]
        [Required]
        public IFormFile Image { get; set; }
    }
}
