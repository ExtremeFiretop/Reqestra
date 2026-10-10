using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Ombi.Core.Authentication;
using Ombi.Core.Models;
using Ombi.Core.Senders;
using Ombi.Notifications;
using Ombi.Notifications.Models;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ombi.Core.Tests.Senders
{
    [TestFixture]
    public class MassEmailSenderTests
    {

        private MassEmailSender _subject;
        private AutoMocker _mocker;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _subject = _mocker.CreateInstance<MassEmailSender>();
        }

        [Test]
        public async Task SendMassEmail_SingleUser()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Users = new List<OmbiUser>
                {
                    new OmbiUser
                    {
                        Id = "a"
                    }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                    Email = "Test@test.com"
                }
            }.AsQueryable().BuildMock());

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.True);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.Subject == model.Subject
            && m.Message == model.Body
            && m.To == "Test@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task SendMassEmail_MultipleUsers()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Users = new List<OmbiUser>
                {
                    new OmbiUser
                    {
                        Id = "a"
                    },
                    new OmbiUser
                    {
                        Id = "b"
                    }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                    Email = "Test@test.com"
                },
                new OmbiUser
                {
                    Id = "b",
                    Email = "b@test.com"
                }
            }.AsQueryable().BuildMock());

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.True);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.Subject == model.Subject
            && m.Message == model.Body
            && m.To == "Test@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.Subject == model.Subject
            && m.Message == model.Body
            && m.To == "b@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task SendMassEmail_RetriesTransientFailure()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Users = new List<OmbiUser>
                {
                    new OmbiUser { Id = "a" }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                    UserName = "user-a",
                    Email = "a@test.com"
                }
            }.AsQueryable().BuildMock());

            var attempts = 0;
            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.SendAdHoc(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()))
                .Returns(() =>
                {
                    attempts++;
                    if (attempts == 1)
                    {
                        throw new IOException("temporary SMTP transport failure");
                    }

                    return Task.CompletedTask;
                });

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.True);
            Assert.That(attempts, Is.EqualTo(2));
        }

        [Test]
        public async Task SendMassEmail_ContinuesAfterPermanentFailure()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Users = new List<OmbiUser>
                {
                    new OmbiUser { Id = "a" },
                    new OmbiUser { Id = "b" }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                    UserName = "user-a",
                    Email = "a@test.com"
                },
                new OmbiUser
                {
                    Id = "b",
                    UserName = "user-b",
                    Email = "b@test.com"
                }
            }.AsQueryable().BuildMock());

            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.To == "a@test.com"), It.IsAny<EmailNotificationSettings>()))
                .ThrowsAsync(new InvalidOperationException("permanent configuration failure"));
            _mocker.GetMock<IEmailProvider>()
                .Setup(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.To == "b@test.com"), It.IsAny<EmailNotificationSettings>()))
                .Returns(Task.CompletedTask);

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.False);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.To == "a@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.To == "b@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task SendMassEmail_UserNoEmail()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Users = new List<OmbiUser>
                {
                    new OmbiUser
                    {
                        Id = "a"
                    }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                }
            }.AsQueryable().BuildMock());

            var result = await _subject.SendMassEmail(model);
            Assert.That(result, Is.True);
            _mocker.Verify<ILogger<MassEmailSender>>(
               x => x.Log(
                   LogLevel.Information,
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);

            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Never);
        }

        [Test]
        public async Task SendMassEmail_Bcc()
        {
            var model = new MassEmailModel
            {
                Body = "Test {Alias} {UserName}",
                Subject = "Subject",
                Bcc = true,
                Users = new List<OmbiUser>
                {
                    new OmbiUser
                    {
                        Id = "a"
                    },
                    new OmbiUser
                    {
                        Id = "b"
                    }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                    Email = "Test@test.com"
                },
                new OmbiUser
                {
                    Id = "b",
                    Email = "b@test.com"
                }
            }.AsQueryable().BuildMock());

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.True);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.Is<NotificationMessage>(m => m.Subject == model.Subject
            && m.Message == "Test User User"
            && m.Other["bcc"] == "Test@test.com,b@test.com"), It.IsAny<EmailNotificationSettings>()), Times.Once);
        }

        [Test]
        public async Task SendMassEmail_Bcc_NoEmails()
        {
            var model = new MassEmailModel
            {
                Body = "Test",
                Subject = "Subject",
                Bcc = true,
                Users = new List<OmbiUser>
                {
                    new OmbiUser
                    {
                        Id = "a"
                    },
                    new OmbiUser
                    {
                        Id = "b"
                    }
                }
            };

            _mocker.Setup<OmbiUserManager, IQueryable<OmbiUser>>(x => x.Users).Returns(new List<OmbiUser>
            {
                new OmbiUser
                {
                    Id = "a",
                },
                new OmbiUser
                {
                    Id = "b",
                }
            }.AsQueryable().BuildMock());

            var result = await _subject.SendMassEmail(model);

            Assert.That(result, Is.True);
            _mocker.Verify<IEmailProvider>(x => x.SendAdHoc(It.IsAny<NotificationMessage>(), It.IsAny<EmailNotificationSettings>()), Times.Never);
        }

    }
}
