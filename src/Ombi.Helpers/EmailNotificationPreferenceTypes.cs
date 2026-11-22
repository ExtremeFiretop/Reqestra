using System.Collections.Generic;
using Ombi.Helpers;

namespace Ombi.Helpers
{
    /// <summary>
    /// Transactional email events that a recipient may opt out of. The supported list
    /// deliberately excludes welcome, password-reset, test, and account-security mail.
    /// Newsletter membership remains represented by the existing ReceivesNewsletter role.
    /// </summary>
    public static class EmailNotificationPreferenceTypes
    {
        public static readonly IReadOnlyList<NotificationType> Supported = new[]
        {
            NotificationType.RequestApproved,
            NotificationType.RequestAvailable,
            NotificationType.PartiallyAvailable,
            NotificationType.RequestDeclined,
            NotificationType.IssueComment,
            NotificationType.IssueResolved,
            NotificationType.Newsletter,
            NotificationType.NewRequest,
            NotificationType.Issue,
            NotificationType.ItemAddedToFaultQueue
        };

        public static bool IsAdministrative(NotificationType type) =>
            type == NotificationType.NewRequest ||
            type == NotificationType.Issue ||
            type == NotificationType.ItemAddedToFaultQueue;

        public static bool IsSupported(NotificationType type) =>
            System.Linq.Enumerable.Contains(Supported, type);
    }
}
