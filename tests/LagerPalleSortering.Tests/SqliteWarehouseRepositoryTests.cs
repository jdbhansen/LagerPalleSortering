using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;
using LagerPalleSortering.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Tests;

public sealed class SqliteWarehouseRepositoryTests
{
    [Fact]
    public async Task UndoLastAsync_WhenLatestEntryDoesNotDepleteItem_UpdatesRemainingQuantity()
    {
        using var harness = await SqliteRepositoryHarness.CreateAsync();

        await harness.Repository.RegisterAsync("ITEM-1", "20260101", 3);
        await harness.Repository.RegisterAsync("ITEM-1", "20260101", 2);

        var undo = await harness.Repository.UndoLastAsync();
        var contents = await harness.Repository.GetPalletContentsAsync("P-001");

        Assert.NotNull(undo);
        Assert.Single(contents);
        Assert.Equal("ITEM-1", contents[0].ProductNumber);
        Assert.Equal(3, contents[0].Quantity);
    }

    [Fact]
    public async Task UndoLastAsync_WhenPalletItemRowIsMissing_StillCompletes()
    {
        using var harness = await SqliteRepositoryHarness.CreateAsync();

        await harness.Repository.RegisterAsync("ITEM-2", "20260101", 1);
        await harness.DeletePalletItemsAsync("P-001");

        var undo = await harness.Repository.UndoLastAsync();
        var pallets = await harness.Repository.GetOpenPalletsAsync();
        var entries = await harness.Repository.GetRecentEntriesAsync(10);

        Assert.NotNull(undo);
        Assert.Empty(pallets);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task BackupDatabaseAsync_WhenDatabaseDoesNotExist_ReturnsEmptyPayload()
    {
        var root = Path.Combine(Path.GetTempPath(), "LagerPalleSorteringRepoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var rules = Options.Create(new WarehouseRulesOptions());
            using var repository = new SqliteWarehouseRepository(
                new SqliteWarehouseDatabaseProvider(new TestWebHostEnvironment(root)),
                rules);

            var backup = await repository.BackupDatabaseAsync();

            Assert.Empty(backup);
        }
        finally
        {
            TryDeleteRoot(root);
        }
    }

    private static void TryDeleteRoot(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
        }
    }

    private sealed class SqliteRepositoryHarness : IDisposable
    {
        private readonly string _root;
        private readonly IWarehouseDatabaseProvider _databaseProvider;

        private SqliteRepositoryHarness(string root, IWarehouseDatabaseProvider databaseProvider, SqliteWarehouseRepository repository)
        {
            _root = root;
            _databaseProvider = databaseProvider;
            Repository = repository;
        }

        public SqliteWarehouseRepository Repository { get; }

        public static async Task<SqliteRepositoryHarness> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "LagerPalleSorteringRepoTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var rules = Options.Create(new WarehouseRulesOptions());
            var databaseProvider = new SqliteWarehouseDatabaseProvider(new TestWebHostEnvironment(root));
            var repository = new SqliteWarehouseRepository(databaseProvider, rules);
            await repository.InitializeAsync();

            return new SqliteRepositoryHarness(root, databaseProvider, repository);
        }

        public async Task DeletePalletItemsAsync(string palletId)
        {
            await using var connection = _databaseProvider.CreateConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM PalletItems WHERE PalletId = $id;";
            command.Parameters.AddWithValue("$id", palletId);
            await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            Repository.Dispose();
            TryDeleteRoot(_root);
        }
    }
}
