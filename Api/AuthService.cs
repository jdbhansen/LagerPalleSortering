using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Api;

public sealed class AuthService
{
    private readonly AuthOptions _options;

    public AuthService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAuthRequired => _options.RequireAuthentication;

    public int SessionTimeoutMinutes => Math.Max(30, _options.SessionTimeoutMinutes);

    public bool ValidateCredentials(string username, string password, out string normalizedUsername)
    {
        normalizedUsername = string.Empty;
        if (!_options.RequireAuthentication)
        {
            normalizedUsername = "anonymous";
            return true;
        }

        var lookup = (username ?? string.Empty).Trim();
        if (lookup.Length == 0 || string.IsNullOrEmpty(password))
        {
            return false;
        }

        var user = _options.Users.FirstOrDefault(
            candidate => string.Equals(candidate.Username?.Trim(), lookup, StringComparison.OrdinalIgnoreCase));
        if (user is null || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrEmpty(user.Password))
        {
            return false;
        }

        if (!FixedTimeEquals(password, user.Password))
        {
            return false;
        }

        normalizedUsername = user.Username.Trim();
        return true;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
