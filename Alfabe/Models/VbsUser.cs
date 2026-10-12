using System;
using System.ComponentModel.DataAnnotations;

namespace Alfabe.Models
{
    public class VbsUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; }

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string UserType { get; set; } // parent | teacher

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }
       
        public bool IsRejected { get; set; } = false;

        public DateTime? RejectedAt { get; set; }

    }
}
