using System.Collections.Generic;
using Ombi.Helpers;

namespace Ombi.Notifications.Models
{
    public class NotificationMessage
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public string To { get; set; }
        // Optional transactional email event; null for welcome, test, or essential email.
        public NotificationType? EmailEventType { get; set; }
        // Prefer a concrete recipient ID over email lookup (e.g. shared addresses).
        public string RecipientUserId { get; set; }

        public Dictionary<string, string> Other { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}