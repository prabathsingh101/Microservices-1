using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Common.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<DashboardChartDto> GetDashboardChartsAsync();

        Task<List<RecentActivityDto>> GetRecentActivitiesAsync();
    }
}
