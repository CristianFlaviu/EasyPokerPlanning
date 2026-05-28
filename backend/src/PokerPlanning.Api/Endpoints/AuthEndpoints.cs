using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Application.Features.GetCurrentUser;

namespace PokerPlanning.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("google/login", GoogleLogin)
            .WithName("GoogleLogin")
            .WithSummary("Begin the Google OAuth handshake.")
            .AllowAnonymous();

        group.MapGet("me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get the signed-in user, or 204 when anonymous.")
            .AllowAnonymous();

        group.MapPost("logout", Logout)
            .WithName("Logout")
            .WithSummary("Sign the caller out and clear the auth cookie.")
            .AllowAnonymous();

        return app;
    }

    private static IResult GoogleLogin(
        string? returnUrl,
        HttpContext http,
        AllowedFrontendOrigins origins,
        GoogleAuthAvailability availability)
    {
        if (!availability.IsConfigured)
        {
            return Results.Problem(
                detail: "Google sign-in is not configured on this server.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Sign-in unavailable",
                type: "Auth.GoogleNotConfigured");
        }

        var redirect = ResolveReturnUrl(returnUrl, origins);
        var props = new AuthenticationProperties { RedirectUri = redirect };
        return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext http,
        IMediator mediator,
        CancellationToken ct)
    {
        if (http.User.Identity?.IsAuthenticated != true)
            return Results.NoContent();

        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Results.NoContent();

        var result = await mediator.Send(new GetCurrentUserQuery(userId), ct);
        if (result.IsFailure)
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }

        return Results.Ok(new CurrentUserResponse(
            result.Value.Id,
            result.Value.Email,
            result.Value.DisplayName,
            result.Value.AvatarUrl));
    }

    private static async Task<IResult> Logout(HttpContext http, CancellationToken ct)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static string ResolveReturnUrl(string? returnUrl, AllowedFrontendOrigins origins)
    {
        var fallback = origins.Exact.FirstOrDefault() ?? "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
            return fallback;

        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
            return returnUrl;

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute))
            return fallback;

        var origin = $"{absolute.Scheme}://{absolute.Authority}";
        if (origins.Exact.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
            return returnUrl;

        if (origins.Wildcard.Any(pattern => WildcardOriginMatches(pattern, absolute)))
            return returnUrl;

        return fallback;
    }

    private static bool WildcardOriginMatches(string pattern, Uri uri)
    {
        if (!Uri.TryCreate(pattern.Replace("*.", "placeholder."), UriKind.Absolute, out var template))
            return false;

        if (!string.Equals(template.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!pattern.Contains("*."))
            return string.Equals(template.Host, uri.Host, StringComparison.OrdinalIgnoreCase);

        var suffix = template.Host[("placeholder.".Length - 1)..];
        return uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Length > suffix.Length;
    }
}

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);
