using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSearchWebsite.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task SendNotificationAsync(string userId, string message)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được để trống.", nameof(userId));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message không được để trống.", nameof(message));

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedDate = DateTime.UtcNow // Sử dụng UTC để đồng bộ thời gian
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(string userId, string message)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được để trống.", nameof(userId));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message không được để trống.", nameof(message));

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedDate = DateTime.UtcNow // Sử dụng UTC để đồng bộ thời gian
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetAllNotificationsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                throw new ArgumentException("Thông báo không tồn tại.", nameof(id));

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetNotificationsByUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được để trống.", nameof(userId));

            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được để trống.", nameof(userId));

            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}