using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly InventoryDbContext _context;
        public NotificationRepository(InventoryDbContext context) => _context = context;

        public async Task<List<NotificationDto>> GetUnreadNotificationsAsync()
        {
            return await _context.AppNotifications
                .AsNoTracking()
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAtFormatted = n.CreatedAt.ToString("dd MMM, hh:mm tt"),
                    TargetUrl = n.TargetUrl
                }).ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync() =>
            await _context.AppNotifications.CountAsync(n => !n.IsRead);

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var notif = await _context.AppNotifications.FindAsync(id);
            if (notif == null) return false;
            notif.IsRead = true;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task AddNotificationAsync(string title, string message, string type, string url)
        {
            var notification = new AppNotification
            {
                Title = title,
                Message = message,
                Type = type,
                TargetUrl = url,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.AppNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            var unread = await _context.AppNotifications.Where(n => !n.IsRead).ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
