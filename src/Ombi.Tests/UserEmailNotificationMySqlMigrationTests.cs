using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NUnit.Framework;
using Ombi.Store.Migrations.OmbiMySql;

namespace Ombi.Tests
{
    [TestFixture]
    public class UserEmailNotificationMySqlMigrationTests
    {
        [Test]
        public void NewPreferencesTable_AvoidsLegacyUserKeyForeignKeyMismatch()
        {
            var migration = new AddUserEmailNotificationPreferences();
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            var up = typeof(AddUserEmailNotificationPreferences).GetMethod(
                "Up", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(up, Is.Not.Null);
            up.Invoke(migration, new object[] { builder });

            var create = builder.Operations.OfType<CreateTableOperation>()
                .Single(x => x.Name == "UserEmailNotificationPreferences");
            Assert.That(create.ForeignKeys, Is.Empty,
                "Older AspNetUsers.Id collations must not block startup migrations.");
            Assert.That(create.Columns.Any(x => x.Name == "UserId"), Is.True);

            var index = builder.Operations.OfType<CreateIndexOperation>()
                .Single(x => x.Table == "UserEmailNotificationPreferences");
            Assert.That(index.IsUnique, Is.True);
            Assert.That(index.Columns, Is.EqualTo(new[] { "UserId", "NotificationType" }));
        }
    }
}
