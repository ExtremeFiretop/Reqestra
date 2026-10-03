using System;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using NUnit.Framework;

namespace Ombi.Notifications.Tests
{
    [TestFixture]
    public class EmailRetryPolicyTests
    {
        [Test]
        public void TemporarySmtpStatus_IsRetryable()
        {
            var exception = new SmtpCommandException(
                SmtpErrorCode.MessageNotAccepted,
                (SmtpStatusCode)451,
                "Temporary local problem");

            Assert.That(EmailRetryPolicy.IsTransient(exception), Is.True);
        }

        [Test]
        public void PermanentSmtpStatus_IsNotRetryable()
        {
            var exception = new SmtpCommandException(
                SmtpErrorCode.MessageNotAccepted,
                (SmtpStatusCode)550,
                "Mailbox unavailable");

            Assert.That(EmailRetryPolicy.IsTransient(exception), Is.False);
        }

        [Test]
        public void TransientSocketFailure_IsRetryable()
        {
            var exception = new SocketException((int)SocketError.TimedOut);

            Assert.That(EmailRetryPolicy.IsTransient(exception), Is.True);
        }

        [Test]
        public void UnrelatedFailure_IsNotRetryable()
        {
            Assert.That(EmailRetryPolicy.IsTransient(new InvalidOperationException("bad configuration")), Is.False);
        }
    }
}
