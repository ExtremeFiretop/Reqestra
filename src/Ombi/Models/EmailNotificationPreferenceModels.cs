using System.Collections.Generic;
using Ombi.Helpers;

namespace Ombi.Models
{
    public class EmailNotificationPreferenceItem
    {
        public NotificationType NotificationType { get; set; }
        public bool Enabled { get; set; }
        public bool GloballyEnabled { get; set; }
        public bool Administrative { get; set; }
    }

    public class EmailNotificationPreferencesViewModel
    {
        public bool EmailEnabled { get; set; }
        public List<EmailNotificationPreferenceItem> Preferences { get; set; } = new List<EmailNotificationPreferenceItem>();
    }

    public class UpdateEmailNotificationPreference
    {
        public NotificationType NotificationType { get; set; }
        public bool Enabled { get; set; }
    }
}
