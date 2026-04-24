using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public NotificationRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<NotificationDto>> GetUnreadNotificationsAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            return await _context.AppNotifications
                .AsNoTracking()
                .Where(n => !n.IsRead && n.CompanyId == companyId && (n.BranchId == null || string.IsNullOrEmpty(branchId) || n.BranchId == branchId))
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

        public async Task<int> GetUnreadCountAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            return await _context.AppNotifications
                .CountAsync(n => !n.IsRead && n.CompanyId == companyId && (n.BranchId == null || string.IsNullOrEmpty(branchId) || n.BranchId == branchId));
        }

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
                CreatedAt = DateTime.Now,
                CompanyId = _currentUserService.CompanyId ?? Guid.Empty,
                BranchId = _currentUserService.BranchId
            };
            _context.AppNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            var unread = await _context.AppNotifications
                .Where(n => !n.IsRead && n.CompanyId == companyId && (n.BranchId == null || string.IsNullOrEmpty(branchId) || n.BranchId == branchId))
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
