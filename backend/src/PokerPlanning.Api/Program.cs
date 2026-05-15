using PokerPlanning.Api.Endpoints;
using PokerPlanning.Application;
using PokerPlanning.Infrastructure;
using PokerPlanning.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PokerPlanningDbContext>("postgres");
builder.AddRedisClient("redis");

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

const string AngularDevCors = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCors, policy => policy
        .WithOrigins("http://localhost:4200")
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
    db.Database.EnsureCreated();
}

app.MapDefaultEndpoints();
app.MapRoomEndpoints();

app.Run();
