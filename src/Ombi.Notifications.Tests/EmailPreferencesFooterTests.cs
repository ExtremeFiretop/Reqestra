using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Ombi.Notifications.Templates;

namespace Ombi.Notifications.Tests
{
    [TestFixture]
    public class EmailPreferencesFooterTests
    {
        private string _templateFile;
        private EmailBasicTemplate _template;

        [SetUp]
        public void SetUp()
        {
            _templateFile = Path.GetTempFileName();
            File.WriteAllText(_templateFile, "<html>{@EMAILPREFERENCES}</html>");
            _template = new EmailBasicTemplate();
            typeof(EmailBasicTemplate).GetField("_templateLocation", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_template, _templateFile);
        }

        [TearDown]
        public void TearDown() => File.Delete(_templateFile);

        [Test]
        public void ValidPreferencesLink_IsHtmlEncoded()
        {
            var result = _template.LoadTemplate("subject", "body", emailPreferencesUrl:
                "https://reqestra.example/user-preferences?tab=notifications&x=y");
            Assert.That(result, Does.Contain("Manage email preferences"));
            Assert.That(result, Does.Contain("&amp;x=y"));
            Assert.That(result, Does.Not.Contain("{@EMAILPREFERENCES}"));
        }

        [TestCase("javascript:alert(1)")]
        [TestCase("data:text/html,attack")]
        [TestCase("https://name:secret@example.test/user-preferences")]
        public void UntrustedUrl_DoesNotRenderLink(string url)
        {
            var result = _template.LoadTemplate("subject", "body", emailPreferencesUrl: url);
            Assert.That(result, Does.Not.Contain("Manage email preferences"));
            Assert.That(result, Does.Not.Contain("{@EMAILPREFERENCES}"));
        }
    }
}
