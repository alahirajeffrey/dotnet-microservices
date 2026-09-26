using LogService.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var mongoHost = builder.Configuration["MongoDb:Host"] ?? "mongodb";
var mongoPort = builder.Configuration["MongoDb:Port"] ?? "27017";
var mongoUsername = builder.Configuration["MongoDb:Username"];
var mongoPassword = builder.Configuration["MongoDb:Password"];
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "logdb";

var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"];

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    if (!string.IsNullOrWhiteSpace(mongoUsername) && !string.IsNullOrWhiteSpace(mongoPassword))
    {
        mongoConnectionString = $"mongodb://{mongoUsername}:{mongoPassword}@{mongoHost}:{mongoPort}/{mongoDatabaseName}?authSource=admin";
    }
    else
    {
        mongoConnectionString = $"mongodb://{mongoHost}:{mongoPort}/{mongoDatabaseName}";
    }
}

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));
builder.Services.AddHostedService<RabbitMqLogConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
