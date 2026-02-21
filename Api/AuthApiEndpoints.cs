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
        IAuthService authService)
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
        return Results.Ok(new AuthStateApiResponse(Authenticated: true, Username: username));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new AuthStateApiResponse(Authenticated: false));
    }

    private static IResult GetCurrentUser(HttpContext httpContext, IAuthService authService)
    {
        if (!authService.IsAuthRequired)
        {
            return Results.Ok(new AuthStateApiResponse(Authenticated: true, Username: "anonymous"));
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new AuthStateApiResponse(Authenticated: false));
        }

        return Results.Ok(new AuthStateApiResponse(Authenticated: true, Username: user.Identity.Name ?? string.Empty));
    }
}
