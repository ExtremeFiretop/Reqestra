using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Ombi.Core.Authentication;
using Ombi.Store.Context;
using Ombi.Store.Entities;
using Ombi.Test.Common;

namespace Ombi.Tests.Middlewear
{
    [TestFixture]
    public class UserActivityMiddlewareTests
    {
        [Test]
        public async Task FailedActivityUpdate_IsContainedInDedicatedScopeAndRequestContinues()
        {
            var user = new OmbiUser
            {
                Id = "activity-user-id",
                UserName = "activity-user",
                UserType = UserType.LocalUser,
                StreamingCountry = "US"
            };

            var userManager = MockHelper.MockUserManager(new List<OmbiUser> { user });
            userManager
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            // Deliberately leave the SQLite schema uncreated so ExecuteUpdateAsync fails.
            // Activity tracking is telemetry and must never abort the real request.
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var dbOptions = new DbContextOptionsBuilder<TestOmbiContext>()
                .UseSqlite(connection)
                .Options;
            using var db = new TestOmbiContext(dbOptions);

            var activityProvider = new Mock<IServiceProvider>();
            activityProvider
                .Setup(x => x.GetService(typeof(OmbiUserManager)))
                .Returns(userManager.Object);
            activityProvider
                .Setup(x => x.GetService(typeof(OmbiContext)))
                .Returns(db);

            var activityScope = new Mock<IServiceScope>();
            activityScope
                .SetupGet(x => x.ServiceProvider)
                .Returns(activityProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory
                .Setup(x => x.CreateScope())
                .Returns(activityScope.Object);

            var nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new UserActivityMiddleware(
                next,
                NullLogger<UserActivityMiddleware>.Instance,
                scopeFactory.Object);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var context = CreateAuthenticatedApiContext(user);

            await middleware.InvokeAsync(context, cache);

            Assert.That(nextCalled, Is.True,
                "A failed LastActive update must never block the user's real request.");
            scopeFactory.Verify(x => x.CreateScope(), Times.Once);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<OmbiUser>()), Times.Never);
            activityScope.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task SuccessfulActivityUpdate_IsStillThrottledBeforeCreatingAnotherScope()
        {
            var user = new OmbiUser
            {
                Id = "activity-user-id",
                UserName = "activity-user",
                UserType = UserType.LocalUser,
                StreamingCountry = "US"
            };

            var userManager = MockHelper.MockUserManager(new List<OmbiUser> { user });
            userManager
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var dbOptions = new DbContextOptionsBuilder<TestOmbiContext>()
                .UseSqlite(connection)
                .Options;
            using var db = new TestOmbiContext(dbOptions);
            db.Database.EnsureCreated();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var activityProvider = new Mock<IServiceProvider>();
            activityProvider
                .Setup(x => x.GetService(typeof(OmbiUserManager)))
                .Returns(userManager.Object);
            activityProvider
                .Setup(x => x.GetService(typeof(OmbiContext)))
                .Returns(db);

            var activityScope = new Mock<IServiceScope>();
            activityScope
                .SetupGet(x => x.ServiceProvider)
                .Returns(activityProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory
                .Setup(x => x.CreateScope())
                .Returns(activityScope.Object);

            var nextCalls = 0;
            RequestDelegate next = _ =>
            {
                nextCalls++;
                return Task.CompletedTask;
            };

            var middleware = new UserActivityMiddleware(
                next,
                NullLogger<UserActivityMiddleware>.Instance,
                scopeFactory.Object);

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var context = CreateAuthenticatedApiContext(user);

            await middleware.InvokeAsync(context, cache);
            await middleware.InvokeAsync(context, cache);

            Assert.That(nextCalls, Is.EqualTo(2));
            scopeFactory.Verify(x => x.CreateScope(), Times.Once);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<OmbiUser>()), Times.Never);

            await db.Entry(user).ReloadAsync();
            Assert.That(user.LastActive, Is.Not.Null);
        }

        private sealed class TestOmbiContext : OmbiContext
        {
            public TestOmbiContext(DbContextOptions<TestOmbiContext> options)
                : base(options)
            {
            }
        }

        private static HttpContext CreateAuthenticatedApiContext(OmbiUser user)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/request/movie";

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("id", user.Id)
            }, "test");

            context.User = new ClaimsPrincipal(identity);
            return context;
        }
    }
}
