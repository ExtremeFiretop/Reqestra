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
        private Mock<IActiveDownloadEtaEstimator> _activeDownloadEtaEstimator;
        private IPostDownloadTransitionTracker _postDownloadTransitionTracker;
        private DownloadStatusService _service;

        [SetUp]
        public void Setup()
        {
            _radarrSettings = new Mock<ISettingsService<RadarrSettings>>();
            _radarr4KSettings = new Mock<ISettingsService<Radarr4KSettings>>();
            _sonarrSettings = new Mock<ISettingsService<SonarrSettings>>();
            _radarrApi = new Mock<IRadarrV3Api>();
            _sonarrApi = new Mock<ISonarrV3Api>();
            _activeDownloadEtaEstimator = new Mock<IActiveDownloadEtaEstimator>();
            _postDownloadTransitionTracker = new PostDownloadTransitionTracker();

            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings { Enabled = false });
            _radarr4KSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new Radarr4KSettings { Enabled = false });
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings { Enabled = false });

            _service = new DownloadStatusService(
                _radarrSettings.Object,
                _radarr4KSettings.Object,
                _sonarrSettings.Object,
                _radarrApi.Object,
                _sonarrApi.Object,
                _activeDownloadEtaEstimator.Object,
                _postDownloadTransitionTracker,
                Mock.Of<ILogger<DownloadStatusService>>());
        }

        [Test]
        public async Task Movie_In_Radarr_Queue_Is_Marked_Downloading_With_Progress_And_Eta()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 32,
                            Timeleft = TimeSpan.FromMinutes(11)
                        }
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
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(68));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(11));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public async Task Active_Movie_Uses_Observed_Eta_Instead_Of_Arr_Queue_Position_Eta()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 5,
                            Timeleft = TimeSpan.FromMinutes(126),
                            DownloadId = "animals",
                            Status = "downloading"
                        }
                    }
                });
            _activeDownloadEtaEstimator
                .Setup(x => x.Observe("Radarr", "animals", 5, It.IsAny<DateTime>()))
                .Returns(TimeSpan.FromMinutes(2));

            var request = new MovieRequests
            {
                TheMovieDbId = 123,
                RequestedDate = DateTime.UtcNow,
                Approved = true,
                Available = false
            };

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(95));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(2));
            _activeDownloadEtaEstimator.Verify(
                x => x.Observe("Radarr", "animals", 5, It.IsAny<DateTime>()),
                Times.Once);
        }

        [Test]
        public async Task Active_Movie_Does_Not_Fall_Back_To_Inflated_Arr_Eta_Before_Rate_Sample_Exists()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 5,
                            Timeleft = TimeSpan.FromMinutes(126),
                            DownloadId = "animals",
                            Status = "downloading"
                        }
                    }
                });
            _activeDownloadEtaEstimator
                .Setup(x => x.Observe("Radarr", "animals", 5, It.IsAny<DateTime>()))
                .Returns((TimeSpan?)null);

            var request = new MovieRequests
            {
                TheMovieDbId = 123,
                RequestedDate = DateTime.UtcNow,
                Approved = true,
                Available = false
            };

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(95));
            Assert.That(request.DownloadEtaMinutes, Is.Null);
        }

        [Test]
        public async Task Queued_Movie_Is_Marked_Queued_And_Keeps_Cumulative_Arr_Eta()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 88,
                            Timeleft = TimeSpan.FromMinutes(126),
                            DownloadId = "queued-movie",
                            Status = "queued"
                        }
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

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.True);
            Assert.That(request.DownloadProgress, Is.EqualTo(12));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(126));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Queued"));
            _activeDownloadEtaEstimator.Verify(
                x => x.Observe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [TestCase("importPending")]
        [TestCase("importing")]
        public async Task Movie_PostDownload_Arr_State_Is_Marked_Importing(string trackedDownloadState)
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 0,
                            DownloadId = "movie-import",
                            Status = "completed",
                            TrackedDownloadState = trackedDownloadState,
                            TrackedDownloadStatus = "ok"
                        }
                    }
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = false
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

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Importing, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.Completed, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(100));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Importing"));
        }

        [Test]
        public async Task Nzbget_PostProcessing_Paused_Row_After_Recent_Active_Transfer_Is_Marked_Importing()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });

            var queueCall = 0;
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    queueCall++;
                    return new RadarrQueueResponse
                    {
                        TotalRecords = 1,
                        Records = new List<RadarrQueueRecord>
                        {
                            new RadarrQueueRecord
                            {
                                Movie = new MovieResponse { tmdbId = 123 },
                                Size = 100,
                                Sizeleft = queueCall == 1 ? 28m : queueCall == 2 ? 5m : 12m,
                                DownloadId = "nzbget-unpack",
                                Status = queueCall == 1 ? "downloading" : "paused",
                                TrackedDownloadState = "downloading",
                                TrackedDownloadStatus = "ok"
                            }
                        }
                    };
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = false
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
            Assert.That(request.DownloadProgress, Is.EqualTo(72));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Importing, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Importing"));

            // Once the active-to-paused handoff is recognized, keep treating the same download id
            // as post-processing even if Arr's remaining-size value moves during NZBGet unpacking.
            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Importing, Is.True);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Importing"));
        }

        [Test]
        public async Task Manually_Paused_Movie_Without_Completed_Transfer_Evidence_Is_Not_Importing()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 40,
                            DownloadId = "manual-pause",
                            Status = "paused",
                            TrackedDownloadState = "downloading",
                            TrackedDownloadStatus = "ok"
                        }
                    }
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = false
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

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Importing, Is.False);
            Assert.That(request.Searching, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        [Test]
        public async Task Manually_Paused_Movie_After_MidDownload_Transfer_Is_Not_Importing()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });

            var queueCall = 0;
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    queueCall++;
                    return new RadarrQueueResponse
                    {
                        TotalRecords = 1,
                        Records = new List<RadarrQueueRecord>
                        {
                            new RadarrQueueRecord
                            {
                                Movie = new MovieResponse { tmdbId = 123 },
                                Size = 100,
                                Sizeleft = 40,
                                DownloadId = "manual-pause-after-active",
                                Status = queueCall == 1 ? "downloading" : "paused",
                                TrackedDownloadState = "downloading",
                                TrackedDownloadStatus = "ok"
                            }
                        }
                    };
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = false
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
            Assert.That(request.DownloadProgress, Is.EqualTo(60));

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Importing, Is.False);
            Assert.That(request.Searching, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        [Test]
        public async Task Arr_Import_Error_Does_Not_Falsely_Mark_Movie_Importing()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 0,
                            DownloadId = "movie-import-error",
                            Status = "completed",
                            TrackedDownloadState = "importPending",
                            TrackedDownloadStatus = "error"
                        }
                    }
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = false,
                        hasFile = false
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

            Assert.That(request.Importing, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        [Test]
        public async Task Arr_Imported_State_Falls_Through_To_Completed_Movie_State()
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
                        new RadarrQueueRecord
                        {
                            Movie = new MovieResponse { tmdbId = 123 },
                            Size = 100,
                            Sizeleft = 0,
                            DownloadId = "movie-imported",
                            Status = "completed",
                            TrackedDownloadState = "imported",
                            TrackedDownloadStatus = "ok"
                        }
                    }
                });
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = true
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

            Assert.That(request.Importing, Is.False);
            Assert.That(request.Completed, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Completed"));
        }

        [Test]
        public async Task Monitored_Movie_Without_File_Is_Marked_Searching()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RadarrQueueResponse());
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = true,
                        hasFile = false
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

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Completed, Is.False);
            Assert.That(request.Searching, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        [Test]
        public async Task Imported_Movie_Waits_As_Completed_Until_Media_Server_Availability()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RadarrQueueResponse());
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = false,
                        hasFile = true
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

            Assert.That(request.Completed, Is.True);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Completed"));

            request.Available = true;
            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Completed, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Available"));
        }

        [Test]
        public async Task Unmonitored_Movie_Without_File_Remains_Processing()
        {
            _radarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new RadarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 7878
            });
            _radarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RadarrQueueResponse());
            _radarrApi.Setup(x => x.GetMovies("key", It.IsAny<string>()))
                .ReturnsAsync(new List<MovieResponse>
                {
                    new MovieResponse
                    {
                        tmdbId = 123,
                        monitored = false,
                        hasFile = false
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

            Assert.That(request.Completed, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
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
                Available = false,
                Queued = true,
                Searching = true,
                Completed = true,
                DownloadProgress = 50,
                DownloadEtaMinutes = 5
            };

            await _service.PopulateMovieDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.Completed, Is.False);
            Assert.That(request.DownloadProgress, Is.Null);
            Assert.That(request.DownloadEtaMinutes, Is.Null);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        [Test]
        public async Task Requested_Tv_Episode_In_Sonarr_Queue_Is_Marked_Downloading_With_Progress_And_Eta()
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
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 25,
                            Timeleft = TimeSpan.FromMinutes(7),
                            DownloadId = "episode-3"
                        }
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(75));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(7));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));
            var episode = request.SeasonRequests[0].Episodes[0];
            Assert.That(episode.Downloading, Is.True);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public async Task Tv_Progress_Is_Size_Weighted_And_Eta_Uses_Longest_Matching_Download()
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
                    TotalRecords = 2,
                    Records = new List<SonarrQueueRecord>
                    {
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 20,
                            Timeleft = TimeSpan.FromMinutes(5),
                            DownloadId = "episode-3",
                            Status = "downloading"
                        },
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 4 },
                            Size = 200,
                            Sizeleft = 100,
                            Timeleft = TimeSpan.FromMinutes(12),
                            DownloadId = "episode-4",
                            Status = "queued"
                        }
                    }
                });

            _activeDownloadEtaEstimator
                .Setup(x => x.Observe("Sonarr", "episode-3", 20, It.IsAny<DateTime>()))
                .Returns(TimeSpan.FromMinutes(5));

            var request = BuildTvRequest(456, 2, 3);
            request.SeasonRequests[0].Episodes.Add(new EpisodeRequests
            {
                EpisodeNumber = 4,
                Requested = true,
                Approved = true,
                Available = false
            });

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(60));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(12));
            Assert.That(request.SeasonRequests[0].Episodes[0].Downloading, Is.True);
            Assert.That(request.SeasonRequests[0].Episodes[0].RequestStatus, Is.EqualTo("Common.Downloading"));
            Assert.That(request.SeasonRequests[0].Episodes[1].Queued, Is.True);
            Assert.That(request.SeasonRequests[0].Episodes[1].RequestStatus, Is.EqualTo("Common.Queued"));
        }

        [Test]
        public async Task Mixed_Active_And_Queued_Tv_Download_Uses_Queued_Eta_While_Active_Sample_Warms_Up()
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
                    TotalRecords = 2,
                    Records = new List<SonarrQueueRecord>
                    {
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 93,
                            Timeleft = TimeSpan.FromMinutes(90),
                            DownloadId = "episode-3",
                            Status = "downloading"
                        },
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 4 },
                            Size = 100,
                            Sizeleft = 100,
                            Timeleft = TimeSpan.FromMinutes(42),
                            DownloadId = "episode-4",
                            Status = "queued"
                        }
                    }
                });
            _activeDownloadEtaEstimator
                .Setup(x => x.Observe("Sonarr", "episode-3", 93, It.IsAny<DateTime>()))
                .Returns((TimeSpan?)null);

            var request = BuildTvRequest(456, 2, 3);
            request.SeasonRequests[0].Episodes.Add(new EpisodeRequests
            {
                EpisodeNumber = 4,
                Requested = true,
                Approved = true,
                Available = false
            });

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(4));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(42));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Downloading"));
        }

        [Test]
        public async Task Active_Tv_Download_Uses_Observed_Eta_Instead_Of_Arr_Queue_Position_Eta()
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
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 9,
                            Timeleft = TimeSpan.FromMinutes(90),
                            DownloadId = "episode-3",
                            Status = "downloading"
                        }
                    }
                });
            _activeDownloadEtaEstimator
                .Setup(x => x.Observe("Sonarr", "episode-3", 9, It.IsAny<DateTime>()))
                .Returns(TimeSpan.FromMinutes(3));

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.EqualTo(91));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(3));
        }

        [Test]
        public async Task Queued_Tv_Download_Is_Marked_Queued_And_Keeps_Cumulative_Arr_Eta()
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
                    TotalRecords = 2,
                    Records = new List<SonarrQueueRecord>
                    {
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 111 },
                            Episode = new Episode { seasonNumber = 1, episodeNumber = 1 },
                            Size = 100,
                            Sizeleft = 25,
                            Timeleft = TimeSpan.FromMinutes(5),
                            DownloadId = "other-active-series",
                            Status = "downloading"
                        },
                        new SonarrQueueRecord
                        {
                            Series = new SonarrSeries { tmdbId = 456 },
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 88,
                            Timeleft = TimeSpan.FromMinutes(42),
                            DownloadId = "queued-episode",
                            Status = "queued"
                        }
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.True);
            Assert.That(request.DownloadProgress, Is.EqualTo(12));
            Assert.That(request.DownloadEtaMinutes, Is.EqualTo(42));
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Queued"));
        }

        [TestCase("importPending")]
        [TestCase("importing")]
        public async Task Tv_PostDownload_Arr_State_Is_Marked_Importing_For_Request_And_Episode(
            string trackedDownloadState)
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
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 3 },
                            Size = 100,
                            Sizeleft = 0,
                            DownloadId = "episode-import",
                            Status = "completed",
                            TrackedDownloadState = trackedDownloadState,
                            TrackedDownloadStatus = "ok"
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        monitored = true,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = false }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 73,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = true,
                        hasFile = false
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Importing, Is.True);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Importing"));

            var episode = request.SeasonRequests[0].Episodes[0];
            Assert.That(episode.Downloading, Is.False);
            Assert.That(episode.Importing, Is.True);
            Assert.That(episode.Queued, Is.False);
            Assert.That(episode.Searching, Is.False);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.Importing"));
        }

        [Test]
        public async Task Individually_Monitored_Tv_Episode_Without_File_Is_Marked_Searching()
        {
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse());
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        tvdbId = 654,
                        monitored = true,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = false }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 73,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = true,
                        hasFile = false
                    },
                    new Episode
                    {
                        id = 74,
                        seasonNumber = 2,
                        episodeNumber = 4,
                        monitored = false,
                        hasFile = false
                    }
                });

            var request = BuildTvRequest(456, 2, 3);
            request.ParentRequest.TvDbId = 654;

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.Completed, Is.False);
            Assert.That(request.Searching, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Searching"));
            var episode = request.SeasonRequests[0].Episodes[0];
            Assert.That(episode.Searching, Is.True);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        [Test]
        public async Task Imported_Tv_Episode_Waits_As_Completed_Until_Media_Server_Availability()
        {
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse());
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        monitored = false,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = false }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 73,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = false,
                        hasFile = true
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Completed, Is.True);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Completed"));
            var episode = request.SeasonRequests[0].Episodes[0];
            Assert.That(episode.Completed, Is.True);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.Completed"));
        }

        [Test]
        public async Task Monitored_Season_Keeps_Future_Releases_In_Searching_Scope()
        {
            var requestedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse());
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        monitored = true,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = true }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 71,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = true,
                        hasFile = true,
                        airDateUtc = requestedAt.AddDays(-7)
                    },
                    new Episode
                    {
                        id = 72,
                        seasonNumber = 2,
                        episodeNumber = 4,
                        monitored = true,
                        hasFile = false,
                        airDateUtc = requestedAt.AddDays(7)
                    }
                });

            var request = BuildTvRequest(456, 2, 3);
            request.RequestedDate = requestedAt;

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Completed, Is.False);
            Assert.That(request.Searching, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Searching"));
        }

        [Test]
        public async Task Unmonitored_Missing_Tv_Episode_Remains_Processing()
        {
            _sonarrSettings.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new SonarrSettings
            {
                Enabled = true,
                ApiKey = "key",
                Ip = "localhost",
                Port = 8989
            });
            _sonarrApi.Setup(x => x.GetQueue("key", It.IsAny<string>(), 1, 1000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SonarrQueueResponse());
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        monitored = true,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = false }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 73,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = false,
                        hasFile = false
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Completed, Is.False);
            Assert.That(request.Searching, Is.False);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
            var episode = request.SeasonRequests[0].Episodes[0];
            Assert.That(episode.Searching, Is.False);
            Assert.That(episode.Completed, Is.False);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
        }

        [Test]
        public async Task Mixed_Tv_Request_Keeps_Imported_Episode_Completed_While_Another_Episode_Is_Queued()
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
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 4 },
                            Size = 100,
                            Sizeleft = 100,
                            Timeleft = TimeSpan.FromMinutes(20),
                            DownloadId = "episode-4",
                            Status = "queued"
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetSeries("key", It.IsAny<string>()))
                .ReturnsAsync(new List<SonarrSeries>
                {
                    new SonarrSeries
                    {
                        id = 7,
                        tmdbId = 456,
                        monitored = true,
                        seasons = new[]
                        {
                            new Season { seasonNumber = 2, monitored = false }
                        }
                    }
                });
            _sonarrApi.Setup(x => x.GetEpisodes(7, "key", It.IsAny<string>()))
                .ReturnsAsync(new List<Episode>
                {
                    new Episode
                    {
                        id = 73,
                        seasonNumber = 2,
                        episodeNumber = 3,
                        monitored = false,
                        hasFile = true
                    },
                    new Episode
                    {
                        id = 74,
                        seasonNumber = 2,
                        episodeNumber = 4,
                        monitored = true,
                        hasFile = false
                    }
                });

            var request = BuildTvRequest(456, 2, 3);
            request.SeasonRequests[0].Episodes.Add(new EpisodeRequests
            {
                EpisodeNumber = 4,
                Requested = true,
                Approved = true,
                Available = false
            });

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Queued, Is.True);
            Assert.That(request.RequestStatus, Is.EqualTo("Common.Queued"));
            Assert.That(request.SeasonRequests[0].Episodes[0].Completed, Is.True);
            Assert.That(request.SeasonRequests[0].Episodes[0].RequestStatus, Is.EqualTo("Common.Completed"));
            Assert.That(request.SeasonRequests[0].Episodes[1].Queued, Is.True);
            Assert.That(request.SeasonRequests[0].Episodes[1].RequestStatus, Is.EqualTo("Common.Queued"));
        }

        [Test]
        public async Task Tv_Status_Refresh_Clears_Stale_Episode_Lifecycle_State()
        {
            var request = BuildTvRequest(456, 2, 3);
            var episode = request.SeasonRequests[0].Episodes[0];
            episode.Downloading = true;
            episode.Importing = true;
            episode.Queued = true;
            episode.Searching = true;
            episode.Completed = true;

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(episode.Downloading, Is.False);
            Assert.That(episode.Importing, Is.False);
            Assert.That(episode.Queued, Is.False);
            Assert.That(episode.Searching, Is.False);
            Assert.That(episode.Completed, Is.False);
            Assert.That(episode.RequestStatus, Is.EqualTo("Common.ProcessingRequest"));
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
                            Episode = new Episode { seasonNumber = 2, episodeNumber = 4 },
                            Size = 100,
                            Sizeleft = 25,
                            Timeleft = TimeSpan.FromMinutes(7)
                        }
                    }
                });

            var request = BuildTvRequest(456, 2, 3);

            await _service.PopulateTvDownloadStatus(new[] { request });

            Assert.That(request.Downloading, Is.False);
            Assert.That(request.Queued, Is.False);
            Assert.That(request.DownloadProgress, Is.Null);
            Assert.That(request.DownloadEtaMinutes, Is.Null);
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
