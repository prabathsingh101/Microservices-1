using Inventory.Application.Common.Interfaces;
using Inventory.Application.DashBoard.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepo;
    private readonly IProductRepository _productRepo;

    public DashboardController(IDashboardRepository dashboardRepo, IProductRepository productRepository)
    {
        _dashboardRepo = dashboardRepo;
        _productRepo = productRepository;
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        // Top 4 widgets ka data return karega
        var summary = await _dashboardRepo.GetDashboardSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("charts")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<ActionResult<DashboardChartDto>> GetChartData()
    {
        // Line chart aur Donut chart ka dynamic data return karega
        var charts = await _dashboardRepo.GetDashboardChartsAsync();
        return Ok(charts);
    }

    [HttpGet("recent-activities")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<ActionResult<List<RecentActivityDto>>> GetRecentActivities()
    {
        // Recent Stock Movements table ke liye dynamic feed
        var activities = await _dashboardRepo.GetRecentActivitiesAsync();
        return Ok(activities);
    }

    [HttpGet("recent-movements")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetRecentMovements([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var data = await _productRepo.GetRecentMovementsPagedAsync(pageNumber, pageSize);
        return Ok(data);
    }
}
