namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "Postgres";

    public string? ConnectionString { get; set; }
}

