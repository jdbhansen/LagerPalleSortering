using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LagerPalleSortering.Api;

public static class AuthApiEndpoints
{
    public static IEndpointRouteBuilder MapAuthApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync).DisableAntiforgery().AllowAnonymous();
        endpoints.MapPost("/auth/logout", (Delegate)LogoutAsync).DisableAntiforgery().AllowAnonymous();
        endpoints.MapGet("/auth/me", GetCurrentUser).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        LoginApiRequest request,
        AuthService authService)
    {
        if (!authService.ValidateCredentials(request.Username ?? string.Empty, request.Password ?? string.Empty, out var username))
        {
            return Results.Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, username),
            new(ClaimTypes.Name, username),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(authService.SessionTimeoutMinutes),
            AllowRefresh = true,
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        return Results.Ok(new { authenticated = true, username });
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { authenticated = false });
    }

    private static IResult GetCurrentUser(HttpContext httpContext, AuthService authService)
    {
        if (!authService.IsAuthRequired)
        {
            return Results.Ok(new { authenticated = true, username = "anonymous" });
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new { authenticated = false });
        }

        return Results.Ok(new
        {
            authenticated = true,
            username = user.Identity.Name ?? string.Empty,
        });
    }
}

public sealed record LoginApiRequest(string? Username, string? Password);
