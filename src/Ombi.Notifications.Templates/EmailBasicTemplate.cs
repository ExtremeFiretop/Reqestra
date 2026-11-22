using System;
using System.IO;
using System.Text;

namespace Ombi.Notifications.Templates
{
    public class EmailBasicTemplate : TemplateBase, IEmailBasicTemplate
    {
        public override string TemplateLocation
        {
            get
            {
                if (string.IsNullOrEmpty(_templateLocation))
                {
                    //_templateLocation = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "net6.0", "Templates",
                    //    "BasicTemplate.html");
                    _templateLocation = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "BasicTemplate.html");
                }
                return _templateLocation;
            }
        }

        private string _templateLocation;

        private const string SubjectKey = "{@SUBJECT}";
        private const string BodyKey = "{@BODY}";
        private const string Poster = "{@POSTER}";
        private const string DateKey = "{@DATENOW}";
        private const string Logo = "{@LOGO}";
        private const string ApplicationNameKey = "{@APPLICATIONNAME}";
        private const string EmailPreferencesKey = "{@EMAILPREFERENCES}";

        public string LoadTemplate(string subject, string body, string imgsrc = default(string), string logo = default(string), string url = default(string), string applicationName = default(string), string emailPreferencesUrl = default(string))
        {
            var sb = new StringBuilder(File.ReadAllText(TemplateLocation));
            sb.Replace(SubjectKey, subject);
            sb.Replace(BodyKey, body);
            sb.Replace(DateKey, DateTime.Now.ToString("f"));
            sb.Replace(Poster, GetPosterContent(imgsrc, url));
            sb.Replace(Logo, string.IsNullOrEmpty(logo) ? OmbiLogo : logo);
            sb.Replace(ApplicationNameKey, GetApplicationName(applicationName));
            sb.Replace(EmailPreferencesKey, GetEmailPreferencesFooter(emailPreferencesUrl));

            return sb.ToString();
        }

        private static string GetApplicationName(string applicationName)
        {
            var displayName = string.IsNullOrWhiteSpace(applicationName) ? "Reqestra" : applicationName.Trim();
            return System.Net.WebUtility.HtmlEncode(displayName);
        }

        private static string GetEmailPreferencesFooter(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(parsed.UserInfo))
            {
                return string.Empty;
            }
            var safeUrl = System.Net.WebUtility.HtmlEncode(parsed.ToString());
            return $"<tr><td style=\"font-size:12px;text-align:center;padding:10px;\">" +
                $"<a href=\"{safeUrl}\">Manage email preferences</a></td></tr>";
        }

        private string GetPosterContent(string imgsrc, string url)
        {
            string posterContent;

            if (string.IsNullOrEmpty(imgsrc))
            {
                posterContent = string.Empty;
            }
            else
            {
                posterContent = $"<img src=\"{imgsrc}\" alt=\"Poster\" width=\"400px\" text-align=\"center\"/>";
                if (!string.IsNullOrEmpty(url))
                {
                    posterContent = $"<a href=\"{url}\">{posterContent}</a>";
                }
                posterContent = $"<tr><td align=\"center\">{posterContent}</td></tr>";
            }
            return posterContent;
        }
    }
}
