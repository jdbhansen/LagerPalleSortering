using LagerPalleSortering.Api;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void ValidateCredentials_WhenAuthDisabled_ReturnsAnonymous()
    {
        var service = CreateService(new AuthOptions
        {
            RequireAuthentication = false
        });

        var valid = service.ValidateCredentials(string.Empty, string.Empty, out var username);

        Assert.True(valid);
        Assert.Equal("anonymous", username);
    }

    [Fact]
    public void ValidateCredentials_WhenAuthEnabledAndCredentialsMatch_ReturnsTrue()
    {
        var service = CreateService(new AuthOptions
        {
            RequireAuthentication = true,
            Users =
            [
                new AuthUserOptions { Username = "admin", Password = "secret" }
            ]
        });

        var valid = service.ValidateCredentials("Admin", "secret", out var username);

        Assert.True(valid);
        Assert.Equal("admin", username);
    }

    [Fact]
    public void ValidateCredentials_WhenPasswordIsWrong_ReturnsFalse()
    {
        var service = CreateService(new AuthOptions
        {
            RequireAuthentication = true,
            Users =
            [
                new AuthUserOptions { Username = "admin", Password = "secret" }
            ]
        });

        var valid = service.ValidateCredentials("admin", "wrong", out var username);

        Assert.False(valid);
        Assert.Equal(string.Empty, username);
    }

    [Fact]
    public void SessionTimeoutMinutes_WhenConfiguredTooLow_UsesMinimum30()
    {
        var service = CreateService(new AuthOptions
        {
            SessionTimeoutMinutes = 5
        });

        Assert.Equal(30, service.SessionTimeoutMinutes);
    }

    private static AuthService CreateService(AuthOptions options)
    {
        return new AuthService(Options.Create(options));
    }
}
