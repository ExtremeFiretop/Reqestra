using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Ombi.Helpers;
using Ombi.Notifications.Agents;
using Ombi.Notifications.Models;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;
using Ombi.Store.Repository;

namespace Ombi.Notifications.Tests
{
    [TestFixture]
    public class EmailNotificationPreferencesTests
    {
        private AutoMocker _mocker;
        private EmailNotification _agent;
        private OmbiUser _recipient;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _agent = _mocker.CreateInstance<EmailNotification>();
            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()))
                .Returns(Task.CompletedTask);
            _recipient = new OmbiUser { Id = "recipient-1", Email = "user@example.test" };
            _mocker.GetMock<UserManager<OmbiUser>>()
                .Setup(x => x.FindByEmailAsync("user@example.test"))
                .ReturnsAsync(_recipient);
        }

        [Test]
        public async Task OptedOutRecipient_DoesNotGetTransactionEmail()
        {
            _mocker.GetMock<IRepository<UserEmailNotificationPreference>>()
                .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEmailNotificationPreference, bool>>>()))
                .ReturnsAsync(new UserEmailNotificationPreference
                {
                    UserId = _recipient.Id,
                    NotificationType = NotificationType.RequestAvailable,
                    Enabled = false
                });

            await Send(NotificationType.RequestAvailable);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Never);
        }

        [Test]
        public async Task ExplicitRecipientId_TakesPrecedenceOverEmailLookup()
        {
            _mocker.GetMock<IRepository<UserEmailNotificationPreference>>()
                .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEmailNotificationPreference, bool>>>()))
                .ReturnsAsync(new UserEmailNotificationPreference
                {
                    UserId = "recipient-1",
                    NotificationType = NotificationType.RequestAvailable,
                    Enabled = false
                });
            var message = new NotificationMessage
            {
                To = "user@example.test", RecipientUserId = "recipient-1",
                EmailEventType = NotificationType.RequestAvailable
            };
            await Invoke(message);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Never);
            _mocker.GetMock<UserManager<OmbiUser>>()
                .Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ExistingUserWithoutPreference_StillReceivesTransactionEmail()
        {
            _mocker.GetMock<IRepository<UserEmailNotificationPreference>>()
                .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEmailNotificationPreference, bool>>>()))
                .ReturnsAsync((UserEmailNotificationPreference)null);

            await Send(NotificationType.RequestApproved);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task ExplicitOptIn_ReceivesTransactionEmail()
        {
            _mocker.GetMock<IRepository<UserEmailNotificationPreference>>()
                .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserEmailNotificationPreference, bool>>>()))
                .ReturnsAsync(new UserEmailNotificationPreference
                {
                    UserId = _recipient.Id,
                    NotificationType = NotificationType.NewRequest,
                    Enabled = true
                });

            await Send(NotificationType.NewRequest);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task NoUserAccount_DoesNotBlockExternalRecipient()
        {
            _mocker.GetMock<UserManager<OmbiUser>>()
                .Setup(x => x.FindByEmailAsync("user@example.test"))
                .ReturnsAsync((OmbiUser)null);

            await Send(NotificationType.IssueComment);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task TestAndEssentialMessages_AreNotSubjectToOptOut()
        {
            await Send(null);
            await Send(NotificationType.Test);
            await Send(NotificationType.WelcomeEmail);
            _mocker.Verify<IEmailProvider>(x => x.Send(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Exactly(3));
            _mocker.GetMock<UserManager<OmbiUser>>()
                .Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void UserControllableTypes_ExcludeTestAndWelcome()
        {
            Assert.That(EmailNotificationPreferenceTypes.IsSupported(NotificationType.Test), Is.False);
            Assert.That(EmailNotificationPreferenceTypes.IsSupported(NotificationType.WelcomeEmail), Is.False);
            Assert.That(EmailNotificationPreferenceTypes.IsSupported(NotificationType.RequestAvailable), Is.True);
            Assert.That(EmailNotificationPreferenceTypes.IsAdministrative(NotificationType.NewRequest), Is.True);
            Assert.That(EmailNotificationPreferenceTypes.IsAdministrative(NotificationType.RequestApproved), Is.False);
        }

        private async Task Send(NotificationType? type)
        {
            var message = new NotificationMessage { To = "user@example.test", EmailEventType = type, Subject = "test", Message = "body" };
            await Invoke(message);
        }

        private async Task Invoke(NotificationMessage message)
        {
            var sendMethod = typeof(EmailNotification).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic);
            await (Task)sendMethod.Invoke(_agent, new object[] { message, new EmailNotificationSettings() });
        }
    }
}
