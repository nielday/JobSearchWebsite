using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobSearchWebsite.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobSearchWebsite.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return View(notifications);
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null)
            {
                return NotFound();
            }
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}