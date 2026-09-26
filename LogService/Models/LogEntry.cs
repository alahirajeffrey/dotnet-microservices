using MongoDB.Bson.Serialization.Attributes;

namespace LogService.Models;

public class LogEntry
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ServiceName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Information";
    public string? UserId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
