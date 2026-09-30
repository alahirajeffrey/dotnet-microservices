using System.Diagnostics;

namespace LogService.Telemetry;

public static class MongoDbTelemetry
{
    public const string SourceName = "DotnetMicroservices.LogService.MongoDB";

    public static ActivitySource Source { get; } = new(SourceName);

    public static void SetDatabaseTags(Activity? activity, string operation)
    {
        activity?.SetTag("db.system.name", "mongodb");
        activity?.SetTag("db.operation.name", operation);
        activity?.SetTag("db.collection.name", "event_logs");
    }
}