using Dapper;
using Identity.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Admin")] // Logs Admin ke liye restrict karne ke liye
    public class SystemLogsController : ControllerBase
    {
        private readonly string _connectionString;

        public SystemLogsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LogsDb")!;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemLogDto>>> GetLogs(
            [FromQuery] string level = null, 
            [FromQuery] string serviceName = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                
                var sql = @"SELECT TOP (@limit) 
                                Id, Message, Level, TimeStamp, Exception, ServiceName, CorrelationId 
                            FROM AppLogs 
                            WHERE 1=1";

                if (!string.IsNullOrEmpty(level))
                    sql += " AND Level = @level";

                if (!string.IsNullOrEmpty(serviceName))
                    sql += " AND ServiceName = @serviceName";

                sql += " ORDER BY TimeStamp DESC";

                var logs = await connection.QueryAsync<SystemLogDto>(sql, new { level, serviceName, limit });
                
                return Ok(logs);
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
                await connection.ExecuteAsync("TRUNCATE TABLE AppLogs");
                return Ok("Logs cleared successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
