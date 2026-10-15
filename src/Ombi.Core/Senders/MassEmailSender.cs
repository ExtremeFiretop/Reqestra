#region Copyright
// /************************************************************************
//    Copyright (c) 2018 Jamie Rees
//    File: MassEmailSender.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ombi.Core.Authentication;
using Ombi.Core.Models;
using Ombi.Core.Settings;
using Ombi.Helpers;
using Ombi.Notifications;
using Ombi.Notifications.Models;
using Ombi.Settings.Settings.Models;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;

namespace Ombi.Core.Senders
{
    public class MassEmailSender : IMassEmailSender
    {
        private static readonly TimeSpan InterEmailDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10)
        };

        public MassEmailSender(IEmailProvider emailProvider, ISettingsService<CustomizationSettings> custom, ISettingsService<EmailNotificationSettings> email,
            ILogger<MassEmailSender> log, OmbiUserManager manager)
        {
            _email = emailProvider;
            _customizationService = custom;
            _emailService = email;
            _log = log;
            _userManager = manager;
        }

        private readonly IEmailProvider _email;
        private readonly ISettingsService<CustomizationSettings> _customizationService;
        private readonly ISettingsService<EmailNotificationSettings> _emailService;
        private readonly ILogger<MassEmailSender> _log;
        private readonly OmbiUserManager _userManager;

        public async Task<bool> SendMassEmail(MassEmailModel model)
        {
            var customization = await _customizationService.GetSettingsAsync();
            var email = await _emailService.GetSettingsAsync();

            return model.Bcc
                ? await SendBccMails(model, customization, email)
                : await SendIndividualEmails(model, customization, email);
        }

        private async Task<bool> SendBccMails(MassEmailModel model, CustomizationSettings customization, EmailNotificationSettings email)
        {
            var resolver = new NotificationMessageResolver();
            var curlys = new NotificationMessageCurlys();

            var validUsers = new List<OmbiUser>();
            foreach (var user in model.Users)
            {
                var fullUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
                if (!fullUser.Email.HasValue())
                {
                    _log.LogInformation("User {UserName} has no email, cannot send mass email to this user", fullUser.UserName);
                    continue;
                }

                validUsers.Add(fullUser);
            }

            if (!validUsers.Any())
            {
                return true;
            }

            var bccAddress = string.Join(',', validUsers.Select(x => x.Email));
            curlys.Setup(new OmbiUser { UserName = "User", Alias = "User" }, customization);
            var template = new NotificationTemplates() { Message = model.Body, Subject = model.Subject };
            var content = resolver.ParseMessage(template, curlys);
            var msg = new NotificationMessage
            {
                Message = content.Message,
                Subject = content.Subject,
                Other = new Dictionary<string, string> { { "bcc", bccAddress } }
            };

            try
            {
                await SendWithRetry(msg, email, "BCC mass email");
                _log.LogInformation("Sent BCC mass email to {RecipientCount} users", validUsers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to send BCC mass email to {RecipientCount} users", validUsers.Count);
                return false;
            }
        }

        private async Task<bool> SendIndividualEmails(MassEmailModel model, CustomizationSettings customization, EmailNotificationSettings email)
        {
            var resolver = new NotificationMessageResolver();
            var curlys = new NotificationMessageCurlys();
            var allSucceeded = true;
            var attemptedSend = false;

            foreach (var user in model.Users)
            {
                var fullUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
                if (!fullUser.Email.HasValue())
                {
                    _log.LogInformation("User {UserName} has no email, cannot send mass email to this user", fullUser.UserName);
                    continue;
                }

                // Space individual SMTP deliveries apart. The previous implementation queued all
                // delayed tasks together, causing every send to wake up and connect at the same time.
                if (attemptedSend)
                {
                    await Task.Delay(InterEmailDelay);
                }
                attemptedSend = true;

                curlys.Setup(fullUser, customization);
                var template = new NotificationTemplates() { Message = model.Body, Subject = model.Subject };
                var content = resolver.ParseMessage(template, curlys);
                var msg = new NotificationMessage
                {
                    Message = content.Message,
                    To = fullUser.Email,
                    Subject = content.Subject
                };

                try
                {
                    await SendWithRetry(msg, email, $"mass email to {fullUser.UserName}");
                    _log.LogInformation("Sent mass email to user {UserName} @ {Email}", fullUser.UserName, fullUser.Email);
                }
                catch (Exception ex)
                {
                    allSucceeded = false;
                    _log.LogError(ex, "Failed to send mass email to user {UserName} @ {Email}", fullUser.UserName, fullUser.Email);
                }
            }

            return allSucceeded;
        }

        private async Task SendWithRetry(NotificationMessage msg, EmailNotificationSettings email, string description)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await _email.SendAdHoc(msg, email);
                    return;
                }
                catch (Exception ex) when (EmailRetryPolicy.IsTransient(ex) && attempt <= RetryDelays.Length)
                {
                    var delay = RetryDelays[attempt - 1];
                    _log.LogWarning(
                        ex,
                        "Transient email failure while sending {Description}. Retrying in {DelaySeconds} seconds (attempt {NextAttempt}/{MaxAttempts})",
                        description,
                        delay.TotalSeconds,
                        attempt + 1,
                        RetryDelays.Length + 1);
                    await Task.Delay(delay);
                }
            }
        }
    }
}
