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
                var parameters = new DynamicParameters();
                parameters.Add("offset", (pageNumber - 1) * pageSize);
                parameters.Add("pageSize", pageSize);

                if (!string.IsNullOrWhiteSpace(level))
                {
                    whereClause += " AND Level = @level";
                    parameters.Add("level", level);
                }

                if (!string.IsNullOrWhiteSpace(serviceName))
                {
                    whereClause += " AND ServiceName = @serviceName";
                    parameters.Add("serviceName", serviceName);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    whereClause += " AND (UPPER(Message) LIKE @search OR UPPER(ServiceName) LIKE @search OR UPPER(Level) LIKE @search)";
                    parameters.Add("search", $"%{search.ToUpper()}%");
                }

                // Get Total Count
                var countSql = $"SELECT COUNT(*) FROM AppLogs {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Sanitize Sort Column
                var allowedColumns = new[] { "Id", "Message", "Level", "TimeStamp", "ServiceName", "CorrelationId" };
                if (!allowedColumns.Contains(sortBy)) sortBy = "TimeStamp";
                if (sortOrder.ToUpper() != "ASC" && sortOrder.ToUpper() != "DESC") sortOrder = "DESC";

                // Get Paginated & Sorted Data
                var dataSql = $@"SELECT Id, Message, Level, TimeStamp, Exception, ServiceName, CorrelationId 
                                FROM AppLogs 
                                {whereClause}
                                ORDER BY {sortBy} {sortOrder}
                                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                var logs = await connection.QueryAsync<SystemLogDto>(dataSql, parameters);
                
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

        [HttpGet("levels")]
        public async Task<ActionResult<IEnumerable<string>>> GetLevels()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT DISTINCT Level FROM AppLogs WHERE Level IS NOT NULL";
                var levels = await connection.QueryAsync<string>(sql);
                return Ok(levels);
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
                await connection.ExecuteAsync("DELETE FROM dbo.AppLogs");
                return Ok(new { message = "Logs cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
