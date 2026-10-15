using NUnit.Framework;
using Ombi.Notifications.Agents;
using Ombi.Settings.Settings.Models.Notifications;

namespace Ombi.Notifications.Tests
{
    [TestFixture]
    public class MobileNotificationConfigurationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void MobileNotification_ValidationHonorsEnabledSetting(bool enabled)
        {
            var subject = new TestableMobileNotification();

            var result = subject.IsConfigurationValid(new MobileNotificationSettings { Enabled = enabled });

            Assert.That(result, Is.EqualTo(enabled));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyMobileNotification_ValidationHonorsEnabledSetting(bool enabled)
        {
            var subject = new TestableLegacyMobileNotification();

            var result = subject.IsConfigurationValid(new MobileNotificationSettings { Enabled = enabled });

            Assert.That(result, Is.EqualTo(enabled));
        }

        private sealed class TestableMobileNotification : MobileNotification
        {
            public TestableMobileNotification()
                : base(null, null, null, null, null, null, null, null, null, null, null, null, null)
            {
            }

            public bool IsConfigurationValid(MobileNotificationSettings settings)
            {
                return ValidateConfiguration(settings);
            }
        }

        private sealed class TestableLegacyMobileNotification : LegacyMobileNotification
        {
            public TestableLegacyMobileNotification()
                : base(null, null, null, null, null, null, null, null, null, null, null, null, null)
            {
            }

            public bool IsConfigurationValid(MobileNotificationSettings settings)
            {
                return ValidateConfiguration(settings);
            }
        }
    }
}
