using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ombi.Api.External.ExternalApis.Radarr;
using Ombi.Api.External.ExternalApis.Radarr.Models;
using Ombi.Api.External.ExternalApis.Radarr.Models.V3;
using Ombi.Api.External.ExternalApis.Sonarr;
using Ombi.Api.External.ExternalApis.Sonarr.Models;
using Ombi.Api.External.ExternalApis.Sonarr.Models.V3;
using Ombi.Core.Services;
using Ombi.Core.Settings;
using Ombi.Settings.Settings.Models.External;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Tests.Services
{
    [TestFixture]
    public class DownloadStatusServiceTests
    {
        private Mock<ISettingsService<RadarrSettings>> _radarrSettings;
        private Mock<ISettingsService<Radarr4KSettings>> _radarr4KSettings;
        private Mock<ISettingsService<SonarrSettings>> _sonarrSettings;
        private Mock<IRadarrV3Api> _radarrApi;
        private Mock<ISonarrV3Api> _sonarrApi;
        private DownloadStatusService _service;

        [SetUp]
        public void Setup()
        {
            _radarrSettings = new Mock<ISettingsService<RadarrSettings>>();
            _radarr4KSettings = new Mock<ISettingsService<Radarr4KSettings>>();
            _sonarrSettings = new Mock<ISettingsService<SonarrSettings>>();
            _radarrApi = new Mock<IRadarrV3Api>();
            _sonarrApi = new Mock<ISonarrV3Api>();

            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings { Enabled = false });
            _radarr4KSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new Radarr4KSettings { Enabled = false });
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings { Enabled = false });

            _service = new DownloadStatusService(
                _radarrSettings.Object,
                _radarr4KSettings.Object,
                _sonarrSettings.Object,
                _radarrApi.Object,
                _sonarrApi.Object,
                Mock.Of<ILogger<DownloadStatusService>>());
        }

        [Test]
        public async Task Movie_In_Radarr_Queue_Is_Marked_Downloading()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RadarrQueueResponse
                {
                    TotalRecords = 1,
                    Records = new List<RadarrQueueRecord>
                    {
                        new RadarrQueueRecord { Movie = new MovieResponse { tmdbId = 123 } }
                    }
                });

            var request = new MovieRequests
            {
                TheMovieDbId = 123,
                RequestedDate = DateTime.UtcNow,
                Approved = true,
                Available = false
            };

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public async Task Movie_Queue_Failure_Falls_Back_To_Processing_Status()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Radarr unavailable"));

            var request = new MovieRequests
            {
                TheMovieDbId = 123,
                RequestedDate = DateTime.UtcNow,
                Approved = true,
                Available = false
            };

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        [Test]
        public async Task Requested_Tv_Episode_In_Sonarr_Queue_Is_Marked_Downloading()
        {
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse
                {
                    TotalRecords = 1,
                    Records = new List<SonarrQueueRecord>
                    {
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 }
                        }
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public async Task Unrequested_Tv_Episode_Does_Not_Mark_Request_Downloading()
        {
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse
                {
                    TotalRecords = 1,
                    Records = new List<SonarrQueueRecord>
                    {
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 4 }
                        }
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        private static ChildRequests BuildTvRequest(int tmdbId, int seasonNumber, int episodeNumber)
        {
            return new ChildRequests
            {
                Approved = true,
                Available = false,
                ParentRequest = new TvRequests { ExternalProviderId = tmdbId },
                SeasonRequests = new List<SeasonRequests>
                {
                    new SeasonRequests
                    {
                        SeasonNumber = seasonNumber,
                        Episodes = new List<EpisodeRequests>
                        {
                            new EpisodeRequests
                            {
                                EpisodeNumber = episodeNumber,
                                Requested = true,
                                Approved = true,
                                Available = false
                            }
                        }
                    }
                }
            };
        }
    }
}
