using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Common.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<NotificationDto>> GetUnreadNotificationsAsync();
        Task<int> GetUnreadCountAsync();
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadAsync();

        // Modules ise call karke notification generate karenge
        Task AddNotificationAsync(string title, string message, string type, string url, string? branchId = null, Guid? companyId = null);
    }
}
