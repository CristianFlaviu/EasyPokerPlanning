using PokerPlanning.Api.Endpoints;
using PokerPlanning.Api.Hubs;
using PokerPlanning.Api.Realtime;
using PokerPlanning.Application;
using PokerPlanning.Application.Abstractions.Realtime;
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
builder.Services.AddInfrastructure();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRoomNotifier, RoomNotifier>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

const string AppCors = "AppCors";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:4200", "http://localhost:4201", "http://localhost:4301" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppCors, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("Poker Planning API")
    .WithTheme(ScalarTheme.Mars));

app.UseCors(AppCors);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokerPlanningDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapRoomEndpoints();
app.MapHub<RoomHub>("/hubs/rooms");

app.Run();
