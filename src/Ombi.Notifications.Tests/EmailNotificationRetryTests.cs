using System;
using System.Reflection;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Ombi.Notifications.Agents;
using Ombi.Notifications.Models;
using Ombi.Settings.Settings.Models.Notifications;

namespace Ombi.Notifications.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class EmailNotificationRetryTests
    {
        private AutoMocker _mocker;
        private EmailNotification _subject;
        private TimeSpan[] _retryDelays;
        private TimeSpan[] _originalRetryDelays;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _subject = _mocker.CreateInstance<EmailNotification>();

            var retryDelaysField = typeof(EmailNotification).GetField("RetryDelays", BindingFlags.NonPublic | BindingFlags.Static);
            _retryDelays = (TimeSpan[])retryDelaysField.GetValue(null);
            _originalRetryDelays = (TimeSpan[])_retryDelays.Clone();

            for (var i = 0; i < _retryDelays.Length; i++)
            {
                _retryDelays[i] = TimeSpan.Zero;
            }
        }

        [TearDown]
        public void TearDown()
        {
            Array.Copy(_originalRetryDelays, _retryDelays, _retryDelays.Length);
        }

        [Test]
        public async Task Send_RetriesTemporarySmtpFailure_ForSameRecipient()
        {
            var message = new NotificationMessage
            {
                To = "requester@test.com",
                Subject = "Partially available",
                Message = "Body"
            };
            var settings = new EmailNotificationSettings();
            var attempts = 0;

            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.Send(message, settings))
                .Returns(() =>
                {
                    attempts++;
                    if (attempts == 1)
                    {
                        throw TemporarySmtpFailure();
                    }

                    return Task.CompletedTask;
                });

            await InvokeSend(message, settings);

            Assert.That(attempts, Is.EqualTo(2));
            _mocker.Verify<IEmailProvider>(x => x.Send(message, settings), Times.Exactly(2));
        }

        [Test]
        public void Send_DoesNotRetryPermanentSmtpFailure()
        {
            var message = new NotificationMessage
            {
                To = "requester@test.com",
                Subject = "Partially available",
                Message = "Body"
            };
            var settings = new EmailNotificationSettings();
            var failure = new SmtpCommandException(
                SmtpErrorCode.MessageNotAccepted,
                (SmtpStatusCode)550,
                "Mailbox unavailable");

            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.Send(message, settings))
                .ThrowsAsync(failure);

            var thrown = Assert.ThrowsAsync<SmtpCommandException>(async () => await InvokeSend(message, settings));

            Assert.That(thrown, Is.SameAs(failure));
            _mocker.Verify<IEmailProvider>(x => x.Send(message, settings), Times.Once);
        }

        [Test]
        public void Send_RethrowsTemporarySmtpFailure_AfterRetryBudgetIsExhausted()
        {
            var message = new NotificationMessage
            {
                To = "requester@test.com",
                Subject = "Partially available",
                Message = "Body"
            };
            var settings = new EmailNotificationSettings();
            var failure = TemporarySmtpFailure();

            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.Send(message, settings))
                .ThrowsAsync(failure);

            var thrown = Assert.ThrowsAsync<SmtpCommandException>(async () => await InvokeSend(message, settings));

            Assert.That(thrown, Is.SameAs(failure));
            _mocker.Verify<IEmailProvider>(x => x.Send(message, settings), Times.Exactly(3));
        }

        private async Task InvokeSend(NotificationMessage message, EmailNotificationSettings settings)
        {
            var sendMethod = typeof(EmailNotification).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task)sendMethod.Invoke(_subject, new object[] { message, settings });
            await task;
        }

        private static SmtpCommandException TemporarySmtpFailure()
        {
            return new SmtpCommandException(
                SmtpErrorCode.MessageNotAccepted,
                (SmtpStatusCode)421,
                "4.3.0 Temporary System Problem. Try again later.");
        }
    }
}
