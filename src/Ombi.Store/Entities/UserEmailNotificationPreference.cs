using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ombi.Helpers;

namespace Ombi.Store.Entities
{
    [Table("UserEmailNotificationPreferences")]
    public class UserEmailNotificationPreference : Entity
    {
        [Required]
        public string UserId { get; set; }
        public NotificationType NotificationType { get; set; }
        public bool Enabled { get; set; }
    }
}
