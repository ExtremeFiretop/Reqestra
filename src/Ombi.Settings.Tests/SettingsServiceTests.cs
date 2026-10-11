using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Helpers;
using Ombi.Settings.Settings;
using Ombi.Settings.Settings.Models.Notifications;
using Ombi.Store.Entities;
using Ombi.Store.Repository;

namespace Ombi.Settings.Tests
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private Mock<ISettingsRepository> _mockRepo;
        private Mock<ICacheService> _mockCache;
        private SettingsService<TheMovieDbSettings> _service;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ISettingsRepository>();
            _mockCache = new Mock<ICacheService>();
            _service = new SettingsService<TheMovieDbSettings>(_mockRepo.Object, _mockCache.Object);
        }

        [Test]
        public async Task GetSettingsAsync_WhenDatabaseRecordExistsWithEmptyContent_ReturnsDefaultInstance()
        {
            // Arrange - Database record exists but Content is empty
            var emptyRecord = new GlobalSettings
            {
                Id = 1,
                SettingsName = "TheMovieDbSettings",
                Content = "" // Empty content
            };

            _mockCache.Setup(x => x.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<System.Func<Task<TheMovieDbSettings>>>(),
                It.IsAny<System.DateTimeOffset>()))
                .Returns<string, System.Func<Task<TheMovieDbSettings>>, System.DateTimeOffset>(
                    async (key, factory, expiration) => await factory());

            _mockRepo.Setup(x => x.GetAsync("TheMovieDbSettings"))
                .ReturnsAsync(emptyRecord);

            // Act
            var result = await _service.GetSettingsAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Should return default instance, not null");
            Assert.That(result, Is.InstanceOf<TheMovieDbSettings>());
        }

        [Test]
        public async Task GetSettingsAsync_WhenDatabaseRecordExistsWithNullContent_ReturnsDefaultInstance()
        {
            // Arrange - Database record exists but Content is null
            var nullContentRecord = new GlobalSettings
            {
                Id = 1,
                SettingsName = "TheMovieDbSettings",
                Content = null // Null content
            };

            _mockCache.Setup(x => x.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<System.Func<Task<TheMovieDbSettings>>>(),
                It.IsAny<System.DateTimeOffset>()))
                .Returns<string, System.Func<Task<TheMovieDbSettings>>, System.DateTimeOffset>(
                    async (key, factory, expiration) => await factory());

            _mockRepo.Setup(x => x.GetAsync("TheMovieDbSettings"))
                .ReturnsAsync(nullContentRecord);

            // Act
            var result = await _service.GetSettingsAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Should return default instance, not null");
            Assert.That(result, Is.InstanceOf<TheMovieDbSettings>());
        }

        [Test]
        public async Task GetSettingsAsync_WhenNoDatabaseRecordExists_ReturnsDefaultInstance()
        {
            // Arrange - No database record exists
            _mockCache.Setup(x => x.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<System.Func<Task<TheMovieDbSettings>>>(),
                It.IsAny<System.DateTimeOffset>()))
                .Returns<string, System.Func<Task<TheMovieDbSettings>>, System.DateTimeOffset>(
                    async (key, factory, expiration) => await factory());

            _mockRepo.Setup(x => x.GetAsync("TheMovieDbSettings"))
                .ReturnsAsync((GlobalSettings)null);

            // Act
            var result = await _service.GetSettingsAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Should return default instance when no record exists");
            Assert.That(result, Is.InstanceOf<TheMovieDbSettings>());
        }

        [Test]
        public async Task MobileNotificationSettings_Enabled_RoundTripsThroughSettingsService()
        {
            // Arrange
            GlobalSettings storedSettings = null;
            var repo = new Mock<ISettingsRepository>();
            var cache = new Mock<ICacheService>();

            cache.Setup(x => x.GetOrAddAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<MobileNotificationSettings>>>(),
                    It.IsAny<DateTimeOffset>()))
                .Returns<string, Func<Task<MobileNotificationSettings>>, DateTimeOffset>(
                    async (key, factory, expiration) => await factory());

            repo.Setup(x => x.GetAsync("MobileNotificationSettings"))
                .Returns(() => Task.FromResult(storedSettings));

            repo.Setup(x => x.InsertAsync(It.IsAny<GlobalSettings>()))
                .ReturnsAsync((GlobalSettings settings) =>
                {
                    storedSettings = settings;
                    return settings;
                });

            var service = new SettingsService<MobileNotificationSettings>(repo.Object, cache.Object);

            // Act
            var saved = await service.SaveSettingsAsync(new MobileNotificationSettings { Enabled = true });
            var reloaded = await service.GetSettingsAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(storedSettings, Is.Not.Null);
                Assert.That(storedSettings.Content, Does.Contain("\"Enabled\":true"));
                Assert.That(reloaded.Enabled, Is.True);
            });
        }
    }
}
