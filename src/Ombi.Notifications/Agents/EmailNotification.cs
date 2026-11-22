using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using Ombi.Core.Settings;
using Ombi.Helpers;
using Ombi.Notifications.Models;
using Ombi.Notifications.Templates;
using Ombi.Settings.Settings.Models;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;
using Ombi.Store.Repository;
using Ombi.Store.Repository.Requests;

namespace Ombi.Notifications.Agents
{
    public class EmailNotification : BaseNotification<EmailNotificationSettings>, IEmailNotification
    {
        public EmailNotification(ISettingsService<EmailNotificationSettings> settings, INotificationTemplatesRepository r, IMovieRequestRepository m, ITvRequestRepository t, IEmailProvider prov, ISettingsService<CustomizationSettings> c,
            ILogger<EmailNotification> log, UserManager<OmbiUser> um, IRepository<RequestSubscription> sub, IMusicRequestRepository music,
            IRepository<UserNotificationPreferences> userPref,
            IRepository<UserEmailNotificationPreference> emailPreferences) : base(settings, r, m, t, c, log, sub, music, userPref, um)
        {
            EmailProvider = prov;
            Logger = log;
            _userManager = um;
            _emailPreferences = emailPreferences;
        }
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10)
        };

        private IEmailProvider EmailProvider { get; }
        private ILogger<EmailNotification> Logger { get; }
        public override string NotificationName => nameof(EmailNotification);
        private readonly UserManager<OmbiUser> _userManager;
        private readonly IRepository<UserEmailNotificationPreference> _emailPreferences;

        private string GetEmailPreferencesUrl()
        {
            var url = Customization?.ApplicationUrl;
            return Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                   (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp) &&
                   string.IsNullOrEmpty(parsed.UserInfo)
                ? url.TrimEnd('/') + "/user-preferences?tab=notifications"
                : null;
        }

        protected override bool ValidateConfiguration(EmailNotificationSettings settings)
        {
            if (!settings.Enabled)
            {
                return false;
            }
            if (settings.Authentication)
            {
                if (string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password))
                {
                    return false;
                }
            }
            if (string.IsNullOrEmpty(settings.Host) || string.IsNullOrEmpty(settings.Port.ToString()))
            {
                return false;
            }

            return true;
        }

        private async Task<NotificationMessage> LoadTemplate(NotificationType type, NotificationOptions model, EmailNotificationSettings settings)
        {
            var parsed = await LoadTemplate(NotificationAgent.Email, type, model);
            if (parsed.Disabled)
            {
                Logger.LogInformation($"Template {type} is disabled for {NotificationAgent.Email}");
                return null;
            }
            var email = new EmailBasicTemplate();
            var html = email.LoadTemplate(parsed.Subject, parsed.Message, parsed.Image,
                Customization.Logo, parsed.DetailsUrl, Customization.ApplicationName,
                type == NotificationType.Test ? null : GetEmailPreferencesUrl());


            var message = new NotificationMessage
            {
                Message = html,
                Subject = parsed.Subject,
                EmailEventType = type,
            };


            return message;
        }

        private async Task<string> LoadPlainTextMessage(NotificationType type, NotificationOptions model, EmailNotificationSettings settings)
        {
            var parsed = await LoadTemplate(NotificationAgent.Email, type, model);

            var preferencesUrl = GetEmailPreferencesUrl();
            return string.IsNullOrEmpty(preferencesUrl) || type == NotificationType.Test
                ? parsed.Message
                : parsed.Message + "\n\nManage email preferences: " + preferencesUrl;
        }

        protected override async Task NewRequest(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.NewRequest, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.NewRequest, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            foreach (var recipient in (await GetPrivilegedUsers()).DistinctBy(x => x.Email))
            {
                if (recipient.Email.IsNullOrEmpty())
                {
                    continue;
                }
                message.To = recipient.Email;
                message.RecipientUserId = recipient.Id;
                await Send(message, settings);
            }
        }

        protected override async Task NewIssue(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.Issue, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.Issue, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            // Issues should be sent to admin
            await SendToAdmins(message, settings);
        }

        protected override async Task IssueComment(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.IssueComment, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.IssueComment, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            if (model.Substitutes.TryGetValue("AdminComment", out var isAdminString) && !bool.Parse(isAdminString))
            {
                await SendToAdmins(message, settings);
            }
            else
            {
                message.To = model.Recipient;
                await Send(message, settings);
            }

        }

        protected override async Task IssueResolved(NotificationOptions model, EmailNotificationSettings settings)
        {
            if (!model.Recipient.HasValue())
            {
                return;
            }
            var message = await LoadTemplate(NotificationType.IssueResolved, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.IssueResolved, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            // Issues resolved should be sent to the user
            message.To = model.Recipient;

            await Send(message, settings);
        }

        protected override async Task AddedToRequestQueue(NotificationOptions model, EmailNotificationSettings settings)
        {
            if (!model.Recipient.HasValue())
            {
                return;
            }
            var message = await LoadTemplate(NotificationType.ItemAddedToFaultQueue, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.ItemAddedToFaultQueue, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            await SendToAdmins(message, settings);
        }

        protected override async Task RequestDeclined(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.RequestDeclined, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.RequestDeclined, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            await SendToSubscribers(settings, message);
            message.To = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUser.Email
                : TvRequest.RequestedUser.Email;
            message.RecipientUserId = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUserId
                : TvRequest.RequestedUserId;
            await Send(message, settings);

        }

        protected override async Task PartiallyAvailable(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.PartiallyAvailable, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.PartiallyAvailable, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            await SendToSubscribers(settings, message);
            message.To = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUser.Email
                : TvRequest.RequestedUser.Email;
            message.RecipientUserId = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUserId
                : TvRequest.RequestedUserId;

            await Send(message, settings);
        }

        protected override async Task RequestApproved(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.RequestApproved, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.RequestApproved, model, settings);
            message.Other.Add("PlainTextBody", plaintext);

            await SendToSubscribers(settings, message);

            message.To = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUser.Email
                : TvRequest.RequestedUser.Email;
            message.RecipientUserId = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUserId
                : TvRequest.RequestedUserId;
            await Send(message, settings);
        }

        private async Task SendToSubscribers(EmailNotificationSettings settings, NotificationMessage message)
        {
            if (await Subscribed.AnyAsync())
            {
                foreach (var user in Subscribed)
                {
                    if (user.Email.IsNullOrEmpty())
                    {
                        continue;
                    }

                    message.To = user.Email;
                    message.RecipientUserId = user.Id;

                    await Send(message, settings);
                }
            }
        }

        protected override async Task AvailableRequest(NotificationOptions model, EmailNotificationSettings settings)
        {
            var message = await LoadTemplate(NotificationType.RequestAvailable, model, settings);
            if (message == null)
            {
                return;
            }

            var plaintext = await LoadPlainTextMessage(NotificationType.RequestAvailable, model, settings);
            message.Other.Add("PlainTextBody", plaintext);
            await SendToSubscribers(settings, message);
            message.To = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUser.Email
                : TvRequest.RequestedUser.Email;
            message.RecipientUserId = model.RequestType == RequestType.Movie
                ? MovieRequest.RequestedUserId
                : TvRequest.RequestedUserId;
            await Send(message, settings);
        }

        protected override async Task Send(NotificationMessage model, EmailNotificationSettings settings)
        {
            if (model.EmailEventType.HasValue &&
                EmailNotificationPreferenceTypes.IsSupported(model.EmailEventType.Value) &&
                model.EmailEventType.Value != NotificationType.Newsletter &&
                !string.IsNullOrWhiteSpace(model.To))
            {
                // Use the actual recipient's account, not the requester's account: administrators,
                // subscribers and requesters can independently opt out of the same event.
                var recipientId = model.RecipientUserId;
                if (string.IsNullOrEmpty(recipientId))
                {
                    recipientId = (await _userManager.FindByEmailAsync(model.To.Trim()))?.Id;
                }
                if (!string.IsNullOrEmpty(recipientId))
                {
                    var preference = await _emailPreferences.FirstOrDefaultAsync(x =>
                        x.UserId == recipientId && x.NotificationType == model.EmailEventType.Value);
                    if (preference != null && !preference.Enabled)
                    {
                        Logger.LogDebug("Skipping {EmailEvent} email for opted-out user {UserId}",
                            model.EmailEventType.Value, recipientId);
                        return;
                    }
                }
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await EmailProvider.Send(model, settings);
                    return;
                }
                catch (Exception ex) when (EmailRetryPolicy.IsTransient(ex) && attempt <= RetryDelays.Length)
                {
                    var delay = RetryDelays[attempt - 1];
                    Logger.LogWarning(
                        ex,
                        "Transient email failure while sending notification to {Recipient}. Retrying in {DelaySeconds} seconds (attempt {NextAttempt}/{MaxAttempts})",
                        model.To,
                        delay.TotalSeconds,
                        attempt + 1,
                        RetryDelays.Length + 1);
                    await Task.Delay(delay);
                }
            }
        }
        
        protected async Task SendToAdmins(NotificationMessage message, EmailNotificationSettings settings)
        {
            foreach (var recipient in (await GetAdminUsers()).DistinctBy(x => x.Email))
            {
                if (recipient.Email.IsNullOrEmpty())
                {
                    continue;
                }
                message.To = recipient.Email;
                message.RecipientUserId = recipient.Id;
                await Send(message, settings);
            }
        }

        protected override async Task Test(NotificationOptions model, EmailNotificationSettings settings)
        {
            var email = new EmailBasicTemplate();
            var html = email.LoadTemplate(
                "Test Message",
                "This is just a test! Success!", "", Customization.Logo);
            var message = new NotificationMessage
            {
                Message = html,
                Subject = $"Ombi: Test",
            };

            message.Other.Add("PlainTextBody", "This is just a test! Success!");

            await SendToAdmins(message, settings);
        }
    }
}
