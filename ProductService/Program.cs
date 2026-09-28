using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductService.Data;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ProductService", serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
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

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=productdb;Username=postgres;Password=password"
    )
);

builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<RabbitMqLogPublisher>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["jwt"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

