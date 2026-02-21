namespace LagerPalleSortering.Api;

public interface IAuthService
{
    bool IsAuthRequired { get; }
    int SessionTimeoutMinutes { get; }
    bool ValidateCredentials(string username, string password, out string normalizedUsername);
}

