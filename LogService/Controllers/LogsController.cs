using LogService.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;

namespace LogService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public LogsController(IMongoDatabase database)
    {
        _database = database;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? serviceName = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100.");
        }

        var collection = _database.GetCollection<LogEntry>("event_logs");
        var filter = serviceName is null
            ? FilterDefinition<LogEntry>.Empty
            : Builders<LogEntry>.Filter.Eq(x => x.ServiceName, serviceName);

        var totalCount = await collection.CountDocumentsAsync(filter);
        var logs = await collection
            .Find(filter)
            .Sort(Builders<LogEntry>.Sort.Descending(x => x.Timestamp))
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var response = new
        {
            Items = logs,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = (int)totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(response);
    }
}
