using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogService.Models;

public class LogEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ServiceName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "Information";
    public string? UserId { get; set; }
    public object? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
