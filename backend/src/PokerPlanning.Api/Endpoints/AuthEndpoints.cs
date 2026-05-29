using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PokerPlanning.Application.Features.ConsumeEmailLoginToken;
using PokerPlanning.Application.Features.GetCurrentUser;
using PokerPlanning.Application.Features.RequestEmailLogin;
using PokerPlanning.Application.Features.UpdateProfile;
using PokerPlanning.Application.Features.UploadAvatar;
using PokerPlanning.Application.Features.Users;
using PokerPlanning.Domain.Users;

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

        group.MapPost("email/request", RequestEmailLogin)
            .WithName("RequestEmailLogin")
            .WithSummary("Send a one-time email magic link.")
            .AllowAnonymous();

        group.MapGet("email/callback", ConsumeEmailLoginToken)
            .WithName("ConsumeEmailLoginToken")
            .WithSummary("Consume an email magic link and sign in.")
            .AllowAnonymous();

        group.MapPost("me/avatar", UploadAvatar)
            .WithName("UploadAvatar")
            .WithSummary("Upload a profile picture for the signed-in user.")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapPut("me/profile", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("Update the signed-in user's display name and avatar.")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> UploadAvatar(
        IFormFile file,
        HttpContext http,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!TryGetUserId(http, out var userId))
            return Results.Unauthorized();

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadAvatarCommand(userId, stream, file.ContentType, file.Length),
            ct);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Avatar upload failed",
                type: result.Error.Code);
        }

        return Results.Ok(new AvatarUploadResponse(result.Value.AvatarUrl));
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        HttpContext http,
        IMediator mediator,
        CancellationToken ct)
    {
        if (!TryGetUserId(http, out var userId))
            return Results.Unauthorized();

        var result = await mediator.Send(
            new UpdateProfileCommand(userId, request.DisplayName, request.AvatarUrl),
            ct);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Profile update failed",
                type: result.Error.Code);
        }

        // Refresh the auth cookie so name/avatar claims match the updated profile.
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(result.Value));

        return Results.Ok(new CurrentUserResponse(
            result.Value.Id,
            result.Value.Email,
            result.Value.DisplayName,
            result.Value.AvatarUrl));
    }

    private static bool TryGetUserId(HttpContext http, out Guid userId)
    {
        userId = Guid.Empty;
        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
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

    private static async Task<IResult> RequestEmailLogin(
        EmailLoginRequest request,
        HttpContext http,
        IMediator mediator,
        AllowedFrontendOrigins origins,
        CancellationToken ct)
    {
        var returnUrl = ResolveReturnUrl(request.ReturnUrl, origins);
        var callbackBaseUrl = BuildAbsoluteUrl(http, "/auth/email/callback");
        var result = await mediator.Send(
            new RequestEmailLoginCommand(
                request.Mode,
                request.Email,
                request.DisplayName,
                returnUrl,
                callbackBaseUrl),
            ct);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Email sign-in request invalid",
                type: result.Error.Code);
        }

        return Results.Accepted();
    }

    private static async Task<IResult> ConsumeEmailLoginToken(
        string? token,
        HttpContext http,
        IMediator mediator,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Problem(
                detail: EmailLoginTokenErrors.InvalidToken.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid login link",
                type: EmailLoginTokenErrors.InvalidToken.Code);
        }

        var result = await mediator.Send(new ConsumeEmailLoginTokenCommand(token), ct);
        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid login link",
                type: result.Error.Code);
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(result.Value.User));

        return Results.Redirect(result.Value.ReturnUrl);
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

    private static string BuildAbsoluteUrl(HttpContext http, string path) =>
        $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}{path}";

    private static ClaimsPrincipal CreatePrincipal(UserDto user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        if (!string.IsNullOrEmpty(user.AvatarUrl))
            identity.AddClaim(new Claim("picture", user.AvatarUrl));

        return new ClaimsPrincipal(identity);
    }
}

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);

public sealed record EmailLoginRequest(
    string Mode,
    string Email,
    string? DisplayName,
    string? ReturnUrl);

public sealed record UpdateProfileRequest(
    string DisplayName,
    string? AvatarUrl);

public sealed record AvatarUploadResponse(string AvatarUrl);
