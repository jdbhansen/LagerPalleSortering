namespace LagerPalleSortering.Api;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool RequireAuthentication { get; init; } = true;
    public int SessionTimeoutMinutes { get; init; } = 480;
    public List<AuthUserOptions> Users { get; init; } = [];
}

public sealed class AuthUserOptions
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
