using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ombi.Core.Authentication;
using Ombi.Store.Context;
using Ombi.Store.Entities;

namespace Ombi
{
    /// <summary>
    /// Records recent authenticated Ombi activity without changing LastLoggedIn.
    /// Activity writes are throttled so normal page/API traffic does not write to the
    /// user table on every request.
    /// </summary>
    public sealed class UserActivityMiddleware
    {
        private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);
        private const string ActivityCachePrefix = "user-last-active:";
        private readonly RequestDelegate _next;
        private readonly ILogger<UserActivityMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserActivityMiddleware(
            RequestDelegate next,
            ILogger<UserActivityMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
        {
            await RecordActivity(context, cache);
            await _next(context);
        }

        private async Task RecordActivity(HttpContext context, IMemoryCache cache)
        {
            if (!context.Request.Path.StartsWithSegments(new PathString("/api")) ||
                context.User?.Identity?.IsAuthenticated != true ||
                IsApiKeyRequest(context))
            {
                return;
            }

            var userName = context.User.Identity.Name;
            if (string.IsNullOrWhiteSpace(userName) ||
                string.Equals(userName, "API", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Use the same explicit Ombi user-id claim that JWT validation uses in
            // StartupExtensions. Do not prefer ClaimTypes.NameIdentifier here: inbound JWT
            // claim mapping can make that claim ambiguous when both `sub` and the explicit
            // NameIdentifier claim are present.
            var userId = context.User.Claims
                .FirstOrDefault(x => string.Equals(x.Type, "id", StringComparison.OrdinalIgnoreCase))
                ?.Value;
            var cacheIdentity = string.IsNullOrWhiteSpace(userId) ? userName.ToUpperInvariant() : userId;
            var cacheKey = ActivityCachePrefix + cacheIdentity;

            if (cache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            try
            {
                // Keep activity tracking in its own scope. LastActive is telemetry rather than
                // an Identity mutation, so the actual write below targets only that column and
                // deliberately does not participate in Identity's ConcurrencyStamp handling.
                using var activityScope = _scopeFactory.CreateScope();
                var userManager = activityScope.ServiceProvider.GetRequiredService<OmbiUserManager>();
                var db = activityScope.ServiceProvider.GetRequiredService<OmbiContext>();

                OmbiUser user;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    user = await userManager.FindByIdAsync(userId);
                }
                else
                {
                    // This is only a defensive fallback for authenticated principals that do
                    // not expose Ombi's explicit Id claim.
                    user = await userManager.FindByNameAsync(userName);
                }

                if (user == null)
                {
                    _logger.LogWarning(
                        "Could not resolve authenticated Ombi user {UserName} for LastActive tracking (user id claim: {UserId})",
                        userName,
                        userId ?? "<missing>");
                    return;
                }

                if (user.IsSystemUser)
                {
                    return;
                }

                // Only throttle after a real Ombi user has been resolved. The cache is an
                // optimization; the database predicate below is the authoritative throttle and
                // makes simultaneous requests for the same account safe.
                if (cache.TryGetValue(cacheKey, out _))
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var cutoff = now - ActivityWriteInterval;

                // Do not call UserManager.UpdateAsync here. That updates an Identity user and
                // checks/rotates ConcurrencyStamp, so two devices using the same account can race.
                // ExecuteUpdateAsync issues a targeted UPDATE for LastActive only. The cutoff is
                // part of the UPDATE predicate, so concurrent requests do not need an
                // Identity-level optimistic-concurrency update.
                var updated = await db.Users
                    .Where(x => x.Id == user.Id &&
                                (!x.LastActive.HasValue || x.LastActive.Value < cutoff))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.LastActive, (DateTime?)now));

                // Cache only after the database operation succeeds. If the write fails, a later
                // request may retry; if another request already won the race, updated is zero and
                // we still establish the normal one-minute throttle.
                cache.Set(cacheKey, true, ActivityWriteInterval);

                if (updated == 0)
                {
                    _logger.LogDebug(
                        "Skipping LastActive update for Ombi user {UserName}; activity was already recorded recently",
                        user.UserName);
                    return;
                }

                _logger.LogDebug(
                    "Updated LastActive for Ombi user {UserName} ({UserId}) to {LastActive}",
                    user.UserName,
                    user.Id,
                    now);
            }
            catch (Exception ex)
            {
                // Activity tracking must never prevent the user's real request from running.
                // The cache is populated only after a successful database operation, so there is
                // nothing to remove here on failure.
                _logger.LogWarning(ex, "Could not update LastActive for Ombi user {UserName}", userName);
            }
        }

        private static bool IsApiKeyRequest(HttpContext context)
        {
            return context.Request.Headers.Keys.Any(x => string.Equals(x, "ApiKey", StringComparison.OrdinalIgnoreCase)) ||
                   context.Request.Query.ContainsKey("apikey");
        }
    }
}
