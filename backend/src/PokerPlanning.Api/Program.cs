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

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRoomNotifier, RoomNotifier>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

const string AngularDevCors = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCors, policy => policy
        .WithOrigins("http://localhost:4200", "http://localhost:4201", "http://localhost:4301")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Poker Planning API")
        .WithTheme(ScalarTheme.Mars));
    app.UseCors(AngularDevCors);

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PokerPlanningDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapRoomEndpoints();
app.MapHub<RoomHub>("/hubs/rooms");

app.Run();
