using Dapper;
using Identity.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Identity.API.Controllers
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class SystemLogsController : ControllerBase
    {
        private readonly string _connectionString;

        public SystemLogsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LogsDb")!;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<SystemLogDto>>> GetLogs(
            [FromQuery] string level = null, 
            [FromQuery] string serviceName = null,
            [FromQuery] string search = null,
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "TimeStamp",
            [FromQuery] string sortOrder = "DESC")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                
                // Base filter logic
                var whereClause = " WHERE 1=1";
                if (!string.IsNullOrEmpty(level)) whereClause += " AND Level = @level";
                if (!string.IsNullOrEmpty(serviceName)) whereClause += " AND ServiceName = @serviceName";
                if (!string.IsNullOrEmpty(search)) whereClause += " AND (Message LIKE @search OR ServiceName LIKE @search OR Level LIKE @search)";

                // Get Total Count
                var countSql = $"SELECT COUNT(*) FROM AppLogs {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { level, serviceName, search = $"%{search}%" });

                // Sanitize Sort Column (Important for SQL Injection prevention with Dynamic Order By)
                var allowedColumns = new[] { "Id", "Message", "Level", "TimeStamp", "ServiceName", "CorrelationId" };
                if (!allowedColumns.Contains(sortBy)) sortBy = "TimeStamp";
                if (sortOrder.ToUpper() != "ASC" && sortOrder.ToUpper() != "DESC") sortOrder = "DESC";

                // Get Paginated & Sorted Data
                var dataSql = $@"SELECT Id, Message, Level, TimeStamp, Exception, ServiceName, CorrelationId 
                                FROM AppLogs 
                                {whereClause}
                                ORDER BY {sortBy} {sortOrder}
                                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                var offset = (pageNumber - 1) * pageSize;
                var logs = await connection.QueryAsync<SystemLogDto>(dataSql, new { 
                    level, 
                    serviceName, 
                    search = $"%{search}%", 
                    offset, 
                    pageSize 
                });
                
                return Ok(new PaginatedResult<SystemLogDto> { 
                    Items = logs, 
                    TotalCount = totalCount 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<string>>> GetServiceNames()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT DISTINCT ServiceName FROM AppLogs WHERE ServiceName IS NOT NULL";
                var services = await connection.QueryAsync<string>(sql);
                return Ok(services);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearLogs()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                // Explicitly specifying dbo schema just in case
                await connection.ExecuteAsync("DELETE FROM dbo.AppLogs");
                return Ok(new { message = "Logs cleared successfully." });
            }
            catch (Exception ex)
            {
                // Returning full exception as JSON for better frontend diagnostics
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
