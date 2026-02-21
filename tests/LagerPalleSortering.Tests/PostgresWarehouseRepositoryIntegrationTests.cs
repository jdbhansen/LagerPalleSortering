using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LagerPalleSortering.Tests;

[Trait("Category", "PostgresIntegration")]
public sealed class PostgresWarehouseRepositoryIntegrationTests
{
    [Fact]
    public async Task RegisterConfirmAndUndo_WorksAgainstPostgres()
    {
        var baseConnectionString = GetBaseConnectionStringOrSkip();
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return;
        }

        await using var fixture = await PostgresRepositoryFixture.CreateAsync(baseConnectionString);

        var first = await fixture.Repository.RegisterAsync("PG-ITEM", "20261224", 2);
        var confirm = await fixture.Repository.ConfirmLatestUnconfirmedByPalletIdAsync(first.PalletId, DateTime.UtcNow);
        var undo = await fixture.Repository.UndoLastAsync();
        var pallets = await fixture.Repository.GetOpenPalletsAsync();

        Assert.True(first.CreatedNewPallet);
        Assert.NotNull(confirm);
        Assert.NotNull(undo);
        Assert.Equal(first.PalletId, undo.PalletId);
        Assert.Empty(pallets);
    }

    [Fact]
    public async Task BackupAndRestore_RoundTripsDataAgainstPostgres()
    {
        var baseConnectionString = GetBaseConnectionStringOrSkip();
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return;
        }

        await using var fixture = await PostgresRepositoryFixture.CreateAsync(baseConnectionString);

        await fixture.Repository.RegisterAsync("PG-BACKUP", "20270101", 1);
        var backup = await fixture.Repository.BackupDatabaseAsync();

        await fixture.Repository.ClearAllDataAsync();
        await using var stream = new MemoryStream(backup);
        await fixture.Repository.RestoreDatabaseAsync(stream);
        var pallets = await fixture.Repository.GetOpenPalletsAsync();

        Assert.Single(pallets);
        Assert.Equal("PG-BACKUP", pallets[0].ProductNumber);
    }

    private static string? GetBaseConnectionStringOrSkip()
    {
        return Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION_STRING");
    }

    private sealed class PostgresRepositoryFixture : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _schemaName;

        private PostgresRepositoryFixture(
            PostgresWarehouseRepository repository,
            string adminConnectionString,
            string schemaName)
        {
            Repository = repository;
            _adminConnectionString = adminConnectionString;
            _schemaName = schemaName;
        }

        public PostgresWarehouseRepository Repository { get; }

        public static async Task<PostgresRepositoryFixture> CreateAsync(string adminConnectionString)
        {
            var schemaName = $"it_{Guid.NewGuid():N}";
            await using (var conn = new NpgsqlConnection(adminConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";";
                await cmd.ExecuteNonQueryAsync();
            }

            var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                SearchPath = schemaName
            };

            var databaseOptions = Options.Create(new DatabaseOptions
            {
                Provider = "Postgres",
                ConnectionString = builder.ConnectionString
            });

            var configuration = new ConfigurationBuilder().Build();
            var rules = Options.Create(new WarehouseRulesOptions());
            var repository = new PostgresWarehouseRepository(databaseOptions, configuration, rules);
            await repository.InitializeAsync();

            return new PostgresRepositoryFixture(repository, adminConnectionString, schemaName);
        }

        public async ValueTask DisposeAsync()
        {
            Repository.Dispose();

            await using var conn = new NpgsqlConnection(_adminConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
