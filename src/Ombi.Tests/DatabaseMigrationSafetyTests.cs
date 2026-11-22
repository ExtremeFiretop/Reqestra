using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NUnit.Framework;
using Ombi.Store.Context;

namespace Ombi.Tests
{
    /// <summary>
    /// CI safety gate for new *application* database migrations. Historical migrations
    /// predate this gate and remain valid for installations migrating from older releases.
    /// Additive operations are allowed; destructive operations or raw SQL in new Up()
    /// methods require explicit code/test review rather than silently shipping.
    /// This does not replace database backups or provider-specific migration tests.
    /// </summary>
    [TestFixture]
    public class DatabaseMigrationSafetyTests
    {
        // First migration introduced with the November 2026 email-preferences feature.
        // Keep this floor unchanged so every later migration stays under the gate.
        private const string GuardedFromMigrationId = "20261122000100_";
        private const string MySqlProvider = "Pomelo.EntityFrameworkCore.MySql";
        private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
        private const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";

        private static readonly HashSet<string> ProtectedAccountTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AspNetUsers", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins",
            "AspNetUserRoles", "AspNetUserTokens", "AspNetRoleClaims",
            "UserNotificationPreferences", "UserEmailNotificationPreferences",
            "UserQualityProfiles", "NotificationUserId", "RequestSubscription"
        };

        [Test]
        public void NewApplicationMigrations_DoNotSilentlyDeleteOrRewriteExistingData()
        {
            var migrations = typeof(OmbiContext).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(Migration).IsAssignableFrom(type))
                .Select(type => new
                {
                    Type = type,
                    Id = type.GetCustomAttribute<MigrationAttribute>()?.Id,
                    Provider = ProviderFor(type.Namespace)
                })
                .Where(item => item.Provider != null && item.Id != null &&
                    string.CompareOrdinal(item.Id, GuardedFromMigrationId) >= 0)
                .OrderBy(item => item.Id)
                .ThenBy(item => item.Provider)
                .ToList();

            // A guard that silently stops discovering migrations gives false confidence.
            Assert.That(migrations.Select(item => item.Provider).Distinct(),
                Is.EquivalentTo(new[] { MySqlProvider, PostgreSqlProvider, SqliteProvider }),
                "The migration safety gate must discover migrations from all three providers.");
            Assert.That(migrations.Any(item => item.Id == "20261122000100_AddUserEmailNotificationPreferences"),
                Is.True, "The original email-preferences migration must remain covered.");

            foreach (var item in migrations)
            {
                var migration = (Migration)Activator.CreateInstance(item.Type);
                var builder = new MigrationBuilder(item.Provider);
                var up = item.Type.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(up, Is.Not.Null, $"Missing Up() for {item.Type.FullName}.");

                // Evaluate the generated EF operations without opening a real database.
                up.Invoke(migration, new object[] { builder });
                var violations = UnsafeOperations(builder.Operations, item.Provider).ToList();
                Assert.That(violations, Is.Empty,
                    $"Migration {item.Id} ({item.Provider}) is unsafe for existing user data. " +
                    "Review the migration and its data-preservation strategy before release.");
            }
        }

        [Test]
        public void GateRejectsDestructiveOperationsAndRawSql()
        {
            var unsafeOperations = new MigrationOperation[]
            {
                new DropTableOperation { Name = "AspNetUsers" },
                new DropColumnOperation { Table = "MovieRequests", Name = "Title" },
                new DeleteDataOperation { Table = "AspNetUsers" },
                new SqlOperation { Sql = "DELETE FROM AspNetUsers" },
                new AlterColumnOperation { Table = "AspNetUsers", Name = "Id" }
            };

            Assert.That(UnsafeOperations(unsafeOperations, MySqlProvider).Count(), Is.EqualTo(5));
        }

        [Test]
        public void GateRejectsUnverifiedMySqlForeignKeysToExistingAccounts()
        {
            // Regression for the v4.71.1 startup crash: a new FK to a legacy user ID
            // can fail when its MySQL charset/collation differs from the parent key.
            var table = new CreateTableOperation { Name = "NewUserPreferenceTable" };
            table.ForeignKeys.Add(new AddForeignKeyOperation
            {
                Table = table.Name,
                PrincipalTable = "AspNetUsers",
                Name = "FK_NewUserPreferenceTable_AspNetUsers_UserId"
            });

            Assert.That(UnsafeOperations(new MigrationOperation[] { table }, MySqlProvider),
                Is.Not.Empty);
            Assert.That(UnsafeOperations(new MigrationOperation[] { table }, SqliteProvider),
                Is.Empty, "The MySQL collation risk is not a universal FK prohibition.");
        }

        [Test]
        public void GateAllowsAdditiveSchemaOperations()
        {
            var additive = new MigrationOperation[]
            {
                new CreateTableOperation { Name = "NewUserPreferenceTable" },
                new AddColumnOperation { Table = "AspNetUsers", Name = "OptionalNewField" },
                new CreateIndexOperation { Table = "NewUserPreferenceTable", Name = "IX_Example" }
            };

            Assert.That(UnsafeOperations(additive, MySqlProvider), Is.Empty);
        }

        private static string ProviderFor(string migrationNamespace)
        {
            switch (migrationNamespace)
            {
                case "Ombi.Store.Migrations.OmbiMySql": return MySqlProvider;
                case "Ombi.Store.Migrations.OmbiPostgres": return PostgreSqlProvider;
                case "Ombi.Store.Migrations.OmbiSqlite": return SqliteProvider;
                default: return null; // Settings migrations use a separate database.
            }
        }

        private static IEnumerable<string> UnsafeOperations(
            IEnumerable<MigrationOperation> operations, string provider)
        {
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case DropTableOperation dropTable:
                        yield return $"DropTable({dropTable.Name})";
                        break;
                    case DropColumnOperation dropColumn:
                        yield return $"DropColumn({dropColumn.Table}.{dropColumn.Name})";
                        break;
                    case DeleteDataOperation deleteData:
                        yield return $"DeleteData({deleteData.Table})";
                        break;
                    case RenameTableOperation renameTable:
                        yield return $"RenameTable({renameTable.Name})";
                        break;
                    case RenameColumnOperation renameColumn:
                        yield return $"RenameColumn({renameColumn.Table}.{renameColumn.Name})";
                        break;
                    case SqlOperation _:
                        // Raw SQL may hide destructive DDL/DML and cannot be validated
                        // from EF operation types. Require an explicit reviewed exception.
                        yield return "Raw SQL in a new migration requires explicit data-safety review";
                        break;
                    case AlterColumnOperation alterColumn when ProtectedAccountTables.Contains(alterColumn.Table):
                        yield return $"AlterColumn({alterColumn.Table}.{alterColumn.Name})";
                        break;
                    case UpdateDataOperation updateData when ProtectedAccountTables.Contains(updateData.Table):
                        yield return $"UpdateData({updateData.Table})";
                        break;
                    case DropPrimaryKeyOperation dropKey when ProtectedAccountTables.Contains(dropKey.Table):
                        yield return $"DropPrimaryKey({dropKey.Table})";
                        break;
                    case DropForeignKeyOperation dropForeignKey when ProtectedAccountTables.Contains(dropForeignKey.Table):
                        yield return $"DropForeignKey({dropForeignKey.Table})";
                        break;
                    case AlterTableOperation alterTable when ProtectedAccountTables.Contains(alterTable.Name):
                        yield return $"AlterTable({alterTable.Name})";
                        break;
                    case CreateTableOperation createTable when provider == MySqlProvider:
                        foreach (var foreignKey in createTable.ForeignKeys.Where(
                            fk => ProtectedAccountTables.Contains(fk.PrincipalTable)))
                        {
                            yield return $"MySQL CreateTable({createTable.Name}) introduces FK to " +
                                $"{foreignKey.PrincipalTable} without verifying legacy key collation";
                        }
                        break;
                    case AddForeignKeyOperation foreignKey when provider == MySqlProvider &&
                        ProtectedAccountTables.Contains(foreignKey.PrincipalTable):
                        yield return $"MySQL AddForeignKey({foreignKey.Table}->{foreignKey.PrincipalTable}) " +
                            "requires a legacy key collation compatibility review";
                        break;
                }
            }
        }
    }
}
