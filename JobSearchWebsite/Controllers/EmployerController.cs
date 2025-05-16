using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobSearchWebsite.Data;
using JobSearchWebsite.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using JobSearchWebsite.Services;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Sửa lỗi: UserManager thay vì UserManager
        private readonly IEmailSender _emailSender;
        private readonly NotificationService _notificationService;

        public EmployerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _notificationService = notificationService;
        }

        // Dashboard của Employer
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var jobs = await _context.Jobs
                .Where(j => j.UserId == user.Id)
                .ToListAsync();
            var applications = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.User)
                .Where(ja => ja.Job.UserId == user.Id)
                .ToListAsync();

            ViewBag.JobCount = jobs.Count;
            ViewBag.ApplicationCount = applications.Count;
            ViewBag.UnreadNotificationCount = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id && !n.IsRead);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();

            return View(applications);
        }

        // Danh sách thông báo của Employer
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return View(notifications);
        }

        // Xem chi tiết đơn ứng tuyển
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.User)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (application.Job.UserId != user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn này.";
                return Forbid();
            }

            return View(application);
        }

        // Cập nhật trạng thái đơn ứng tuyển
        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus(int id, string status)
        {
            var application = await _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.User)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (application.Job.UserId != user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật đơn này.";
                return Forbid();
            }

            if (status != "Pending" && status != "Viewed" && status != "Approved" && status != "Rejected")
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return BadRequest();
            }

            application.Status = status;
            _context.JobApplications.Update(application);
            await _context.SaveChangesAsync();

            // Thêm thông báo cho ứng viên
            await _notificationService.CreateNotificationAsync(
                application.UserId,
                $"Trạng thái đơn ứng tuyển của bạn cho công việc '{application.Job.Title}' đã được cập nhật thành: {status}"
            );

            // Gửi email thông báo cho ứng viên
            if (application.User != null && !string.IsNullOrEmpty(application.User.Email))
            {
                var currentDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                var statusColor = status == "Approved" ? "#28a745" : status == "Pending" ? "#ffc107" : "#dc3545";
                var statusText = status == "Pending" ? "Chờ xử lý" : status == "Viewed" ? "Đã xem" : status == "Approved" ? "Được duyệt" : "Bị từ chối";
                var body = $@"
    
        
            Thông báo từ JobSearchWebsite

        

        
            Xin chào {application.User.UserName},

            Chúng tôi xin thông báo rằng trạng thái đơn ứng tuyển của bạn cho công việc {application.Job.Title} tại công ty {application.Job.Company} đã được cập nhật:

            
                Trạng thái mới: {statusText}

                Thời gian cập nhật: {currentDate}
            

            Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ thêm, vui lòng liên hệ với chúng tôi qua email support@jobsearchwebsite.com hoặc số điện thoại +84 123 456 789.

            Chúc bạn thành công trong hành trình tìm kiếm việc làm!
Trân trọng,
Đội ngũ JobSearchWebsite

        

        
            © {DateTime.Now.Year} JobSearchWebsite. All rights reserved.

            Chính sách bảo mật | Điều khoản sử dụng

        

    ";
                await _emailSender.SendEmailAsync(application.User.Email, "Cập nhật trạng thái đơn ứng tuyển", body);
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn thành công!";
            return RedirectToAction("ApplicationDetails", new { id });
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null || notification.UserId != _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Thông báo không hợp lệ hoặc bạn không có quyền.";
                return RedirectToAction("Notifications");
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đánh dấu đã đọc thành công!";
            return RedirectToAction("Notifications");
        }
        // Tìm kiếm hồ sơ ứng viên
        public IActionResult SearchCandidates(string keyword)
        {
            var query = _context.UserProfiles.Where(p => p.IsPublic);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.FullName.Contains(keyword) ||
                    p.Skills.Contains(keyword) ||
                    p.Education.Contains(keyword) ||
                    p.Experience.Contains(keyword) ||
                    p.Address.Contains(keyword));
            }

            var result = query.ToList();
            return View(result);
        }
    }
}