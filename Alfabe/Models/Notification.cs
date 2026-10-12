using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alfabe.Models
{
    public enum NotificationAudience
    {
        Parents = 1,
        Teachers = 2,
        Both = 3
    }

    [Table("Notifications")]
    public class Notification
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationAudience Audience { get; set; } = NotificationAudience.Parents;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ScheduledAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        [Required]
        public bool IsPublished { get; set; } = true;
    }
}
