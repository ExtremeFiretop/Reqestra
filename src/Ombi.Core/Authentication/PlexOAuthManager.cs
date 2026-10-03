using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Ombi.Api.External.MediaServers.Plex;
using Ombi.Api.External.MediaServers.Plex.Models;
using Ombi.Api.External.MediaServers.Plex.Models.OAuth;
using Ombi.Core.Settings;
using Ombi.Core.Settings.Models.External;
using Ombi.Helpers;
using Ombi.Settings.Settings.Models;

namespace Ombi.Core.Authentication
{
    public class PlexOAuthManager : IPlexOAuthManager
    {
        public PlexOAuthManager(IPlexApi api, ISettingsService<CustomizationSettings> settings, ISettingsService<PlexSettings> plexSettings, ILogger<PlexOAuthManager> logger, IMemoryCache memoryCache)
        {
            _api = api;
            _customizationSettingsService = settings;
            _plexSettingsService = plexSettings;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        private readonly IPlexApi _api;
        private readonly ISettingsService<CustomizationSettings> _customizationSettingsService;
        private readonly ISettingsService<PlexSettings> _plexSettingsService;
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private const string SessionCachePrefix = "PlexOAuthSession:";

        private sealed class PlexOAuthPinSessionState
        {
            public int PinId { get; init; }
            public string PinCode { get; init; }
        }

        public async Task<OAuthContainer> CreatePin()
        {
            var pin = await _api.CreatePin();
            if (pin?.Result != null && !string.IsNullOrWhiteSpace(pin.Result.code))
            {
                // The numeric Plex PIN id and PIN code are authentication material. Keep both
                // server-side and give the browser only a cryptographically random opaque handle.
                var lifetimeSeconds = pin.Result.expiresIn > 0 ? Math.Min(pin.Result.expiresIn, 1800) : 300;
                var pollToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(lifetimeSeconds),
                    Size = 1
                };

                _memoryCache.Set(
                    GetSessionCacheKey(pollToken),
                    new PlexOAuthPinSessionState
                    {
                        PinId = pin.Result.id,
                        PinCode = pin.Result.code
                    },
                    cacheOptions);

                pin.Result.pollToken = pollToken;
            }

            return pin;
        }

        public async Task<string> GetAccessTokenFromPollToken(string pollToken)
        {
            if (!TryGetPinSession(pollToken, out var session))
            {
                _logger.LogDebug("Plex OAuth poll token was not created by this Ombi instance or its cached PIN session has expired.");
                return string.Empty;
            }

            var pin = await _api.GetPin(session.PinId, session.PinCode);
            if (pin?.Errors != null)
            {
                foreach (var err in pin.Errors?.errors ?? new List<OAuthErrors>())
                {
                    _logger.LogError("Code: '{Code}' : '{Message}'", err.code, err.message);
                }

                return string.Empty;
            }

            if (pin?.Result == null)
            {
                return string.Empty;
            }

            if (pin.Result.expiresIn <= 0)
            {
                _memoryCache.Remove(GetSessionCacheKey(pollToken));
                _logger.LogError("Pin has expired");
                return string.Empty;
            }

            // Sanity log: compare the PIN clientIdentifier with our current InstallId used for X-Plex-Client-Identifier
            try
            {
                var plexSettings = await _plexSettingsService.GetSettingsAsync();
                var installId = plexSettings?.InstallId.ToString("N");
                var pinClientId = pin.Result.clientIdentifier;

                if (string.IsNullOrWhiteSpace(installId))
                {
                    _logger.LogWarning("Plex OAuth sanity check: InstallId is empty; Plex PIN redemption cannot use a stable client identifier.");
                }
                else if (!string.Equals(installId, pinClientId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Plex OAuth sanity check: Mismatch between server InstallId '{InstallIdPrefix}' and PIN.clientIdentifier '{PinClientIdPrefix}'. This can cause Plex PIN polling failures (code 1020).",
                        installId.Length >= 6 ? installId.Substring(0, 6) : installId,
                        pinClientId?.Length >= 6 ? pinClientId.Substring(0, 6) : pinClientId);
                }
                else
                {
                    _logger.LogDebug("Plex OAuth sanity check: Client identifier matches.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Plex OAuth sanity check logging failed");
            }

            if (!string.IsNullOrWhiteSpace(pin.Result.authToken))
            {
                _memoryCache.Remove(GetSessionCacheKey(pollToken));
            }

            return pin.Result.authToken;
        }

        public async Task<PlexAccount> GetAccount(string accessToken)
        {
            return await _api.GetAccount(accessToken);
        }

        public async Task<Uri> GetOAuthUrl(string pollToken, string websiteAddress = null)
        {
            if (!TryGetPinSession(pollToken, out var session))
            {
                _logger.LogDebug("Plex OAuth poll token was not created by this Ombi instance or its cached PIN session has expired.");
                return null;
            }

            var settings = await _customizationSettingsService.GetSettingsAsync();
            return await _api.GetOAuthUrl(session.PinCode, settings.ApplicationUrl.IsNullOrEmpty() ? websiteAddress : settings.ApplicationUrl);
        }

        public async Task<Uri> GetWizardOAuthUrl(string pollToken, string websiteAddress)
        {
            if (!TryGetPinSession(pollToken, out var session))
            {
                _logger.LogDebug("Plex OAuth poll token was not created by this Ombi instance or its cached PIN session has expired.");
                return null;
            }

            return await _api.GetOAuthUrl(session.PinCode, websiteAddress);
        }

        private bool TryGetPinSession(string pollToken, out PlexOAuthPinSessionState session)
        {
            session = null;
            return PlexOAuthPollToken.IsValid(pollToken) &&
                   _memoryCache.TryGetValue(GetSessionCacheKey(pollToken), out session) &&
                   session != null &&
                   session.PinId > 0 &&
                   !string.IsNullOrWhiteSpace(session.PinCode);
        }

        private static string GetSessionCacheKey(string pollToken) => $"{SessionCachePrefix}{pollToken}";
    }
}
