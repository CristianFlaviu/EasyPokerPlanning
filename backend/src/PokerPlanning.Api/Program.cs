using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using PokerPlanning.Api.Endpoints;
using PokerPlanning.Api.Hubs;
using PokerPlanning.Api.Realtime;
using PokerPlanning.Api.Security;
using PokerPlanning.Application;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Features.SignInWithGoogle;
using PokerPlanning.Infrastructure;
using PokerPlanning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PokerPlanningDbContext>(
    "postgres",
    configureDbContextOptions: options => options.UseNpgsql(
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "poker")));
builder.AddRedisClient("redis");

builder.Services.AddApplication(builder.Configuration["MediatR:LicenseKey"]);
var roomAccessTokenSecret = builder.Configuration["RoomAccessToken:Secret"];
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(roomAccessTokenSecret))
    throw new InvalidOperationException("RoomAccessToken:Secret must be configured outside Development.");

builder.Services.AddInfrastructure(roomAccessTokenSecret);
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddSingleton<IRoomNotifier, RoomNotifier>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId)
    && !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        if (googleConfigured)
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "pp.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

if (googleConfigured)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/auth/google/callback";
        options.SaveTokens = false;
        options.Scope.Add("email");
        options.Scope.Add("profile");

        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        options.ClaimActions.MapJsonKey("picture", "picture", "url");

        options.Events.OnTicketReceived = async ctx =>
        {
            var principal = ctx.Principal
                ?? throw new InvalidOperationException("Google ticket missing principal.");

            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Google ticket missing subject.");
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var picture = principal.FindFirstValue("picture")
                ?? principal.FindFirstValue("urn:google:picture");

            var mediator = ctx.HttpContext.RequestServices.GetRequiredService<IMediator>();
            var result = await mediator.Send(
                new SignInWithGoogleCommand(subject, email, name, picture),
                ctx.HttpContext.RequestAborted);

            if (result.IsFailure)
            {
                ctx.Fail(result.Error.Message);
                return;
            }

            var user = result.Value;
            var identity = new ClaimsIdentity(
                authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                identity.AddClaim(new Claim("picture", user.AvatarUrl));

            ctx.Principal = new ClaimsPrincipal(identity);
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddSingleton(new GoogleAuthAvailability(googleConfigured));

const string AppCors = "AppCors";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? [
        "http://localhost:4200",
        "http://localhost:4201",
        "http://localhost:4203",
        "http://localhost:4301",
        "https://poker-planning-online.site",
        "https://easypokerplanning.pages.dev",
    ];
var allowedWildcardOrigins = builder.Configuration
    .GetSection("Cors:AllowedWildcardOrigins")
    .Get<string[]>()
    ?? ["https://*.easypokerplanning.pages.dev"];
var corsOrigins = allowedOrigins
    .Concat(allowedWildcardOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddSingleton(new AllowedFrontendOrigins(allowedOrigins, allowedWildcardOrigins));

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppCors, policy => policy
        .WithOrigins(corsOrigins)
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("Poker Planning API")
    .WithTheme(ScalarTheme.Mars));

app.UseCors(AppCors);

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokerPlanningDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapRoomEndpoints();
app.MapAuthEndpoints();
app.MapFeedbackEndpoints();
app.MapHub<RoomHub>("/hubs/rooms");

app.Run();
