using LogService.Services;
using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "LogService", serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
            .AddHttpClientInstrumentation(options => { options.RecordException = true; });

        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        });
    });

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
