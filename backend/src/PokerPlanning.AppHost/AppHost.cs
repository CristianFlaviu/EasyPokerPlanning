var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddPostgres("postgres-server")
    .WithDataVolume()
    .WithPgAdmin();

var pokerDb = postgresServer.AddDatabase("postgres", databaseName: "pokerplanning");

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

builder.AddProject<Projects.PokerPlanning_Api>("api")
    .WithReference(pokerDb)
    .WaitFor(pokerDb)
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
