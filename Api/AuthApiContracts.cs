namespace LagerPalleSortering.Api;

public sealed record LoginApiRequest(string? Username, string? Password);

public sealed record AuthStateApiResponse(bool Authenticated, string? Username = null);

