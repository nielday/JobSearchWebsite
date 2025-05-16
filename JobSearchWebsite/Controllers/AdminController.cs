using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobSearchWebsite.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using JobSearchWebsite.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System;
using JobSearchWebsite.Services;

namespace JobSearchWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AdminController> _logger;
        private readonly NotificationService _notificationService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<AdminController> logger,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                _logger.LogInformation("Đã tải danh sách {UserCount} người dùng.", users.Count);
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách người dùng.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách người dùng.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Applications()
        {
            try
            {
                var applications = await _context.JobApplications
                    .Include(a => a.Job)
                    .Include(a => a.User)
                    .OrderByDescending(a => a.AppliedDate)
                    .ToListAsync();

                _logger.LogInformation("Đã tải danh sách {ApplicationCount} đơn ứng tuyển.", applications.Count);
                return View(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách đơn ứng tuyển.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách đơn ứng tuyển.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ManageRoles(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound("Người dùng không tồn tại.");

                var userRoles = await _userManager.GetRolesAsync(user);
                var allRoles = await _roleManager.Roles.ToListAsync();

                ViewBag.UserId = user.Id;
                ViewBag.UserName = user.UserName;
                ViewBag.UserRoles = userRoles;
                ViewBag.AllRoles = allRoles;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quản lý vai trò cho người dùng {UserId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin vai trò.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(string userId, List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound("Người dùng không tồn tại.");

                var currentRoles = await _userManager.GetRolesAsync(user);
                var removedRoles = currentRoles.Except(roles ?? new List<string>()).ToList();
                var addedRoles = (roles ?? new List<string>()).Except(currentRoles).ToList();

                if (removedRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, removedRoles);
                if (addedRoles.Any())
                    await _userManager.AddToRolesAsync(user, addedRoles);

                _logger.LogInformation("Đã cập nhật vai trò cho người dùng {UserId}.", userId);
                TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật vai trò cho người dùng {UserId}.", userId);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật vai trò.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Jobs()
        {
            try
            {
                var jobs = _context.Jobs
                    .Include(j => j.User)
                    .OrderBy(j => j.Status == "Chờ duyệt" ? 0 : 1)
                    .ToList();
                _logger.LogInformation("Đã tải danh sách {JobCount} công việc.", jobs.Count);
                return View(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách công việc.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách công việc.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.User)
                    .Include(j => j.Applications)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (job == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy công việc.";
                    return NotFound();
                }

                _logger.LogInformation("Đã tải chi tiết công việc ID {JobId}.", id);
                return View(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết công việc ID {JobId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết công việc.";
                return RedirectToAction("Jobs");
            }
        }

        public async Task<IActionResult> Statistics(int? year, int? month)
        {
            try
            {
                var jobs = _context.Jobs.AsQueryable();
                var users = _userManager.Users.AsQueryable();
                var applications = _context.JobApplications.AsQueryable();

                if (year.HasValue)
                {
                    jobs = jobs.Where(j => j.CreatedDate.Year == year.Value);
                    users = users.Where(u => u.RegisteredDate.Year == year.Value);
                    applications = applications.Where(a => a.AppliedDate.Year == year.Value);
                }

                if (month.HasValue)
                {
                    jobs = jobs.Where(j => j.CreatedDate.Month == month.Value);
                    applications = applications.Where(a => a.AppliedDate.Month == month.Value);
                }

                // Thống kê người dùng
                ViewBag.AdminCount = await users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Join(_context.Roles, ur => ur.ur.RoleId, r => r.Id, (ur, r) => new { ur.u, r })
                    .Where(x => x.r.Name == "Admin")
                    .CountAsync();
                ViewBag.EmployerCount = await users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Join(_context.Roles, ur => ur.ur.RoleId, r => r.Id, (ur, r) => new { ur.u, r })
                    .Where(x => x.r.Name == "Employer")
                    .CountAsync();
                ViewBag.JobSeekerCount = await users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                    .Join(_context.Roles, ur => ur.ur.RoleId, r => r.Id, (ur, r) => new { ur.u, r })
                    .Where(x => x.r.Name == "JobSeeker")
                    .CountAsync();

                // Thống kê công việc
                ViewBag.NewJobCount = await jobs.CountAsync(j => j.Status == "Mới tạo");
                ViewBag.ApprovedJobCount = await jobs.CountAsync(j => j.Status == "Đã duyệt");
                ViewBag.ClosedJobCount = await jobs.CountAsync(j => j.Status == "Đã đóng");

                // Thống kê đơn ứng tuyển
                ViewBag.ApplicationCount = await applications.CountAsync();
                ViewBag.PendingApplicationCount = await applications.CountAsync(a => a.Status == "Chờ xử lý");
                ViewBag.ApprovedApplicationCount = await applications.CountAsync(a => a.Status == "Được duyệt");
                ViewBag.RejectedApplicationCount = await applications.CountAsync(a => a.Status == "Bị từ chối");

                // Thêm thống kê thông báo
                ViewBag.UnreadAdminNotifications = await _context.Notifications
                    .CountAsync(n => n.UserId == _userManager.GetUserId(User) && !n.IsRead);
                ViewBag.TotalNotifications = await _context.Notifications.CountAsync();
                ViewBag.UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);

                // Xu hướng công việc theo tháng
                int selectedYear = year ?? DateTime.Now.Year;
                var jobTrendNew = new int[12];
                var jobTrendApproved = new int[12];

                for (int i = 1; i <= 12; i++)
                {
                    jobTrendNew[i - 1] = await jobs
                        .Where(j => j.CreatedDate.Year == selectedYear && j.CreatedDate.Month == i && j.Status == "Mới tạo")
                        .CountAsync();
                    jobTrendApproved[i - 1] = await jobs
                        .Where(j => j.CreatedDate.Year == selectedYear && j.CreatedDate.Month == i && j.Status == "Đã duyệt")
                        .CountAsync();
                }

                ViewBag.JobTrendNew = jobTrendNew;
                ViewBag.JobTrendApproved = jobTrendApproved;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedMonth = month;

                _logger.LogInformation("Đã tạo thống kê cho năm {Year} và tháng {Month}.", year, month);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thống kê cho năm {Year} và tháng {Month}.", year, month);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo thống kê.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ApproveJob(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.User)
                    .FirstOrDefaultAsync(j => j.Id == id);
                if (job == null) return NotFound("Công việc không tồn tại.");

                job.Status = "Đã duyệt";
                await _notificationService.CreateNotificationAsync(
                    job.UserId,
                    $"Công việc '{job.Title}' đã được duyệt."
                );

                // Tìm JobSeeker có danh mục ưa thích phù hợp với công việc
                var jobSeekers = await _userManager.GetUsersInRoleAsync("JobSeeker");
                foreach (var jobSeeker in jobSeekers)
                {
                    var profile = await _context.UserProfiles
                        .FirstOrDefaultAsync(p => p.Id == jobSeeker.Id);

                    if (profile != null && !string.IsNullOrEmpty(profile.PreferredCategories))
                    {
                        var preferredCategories = profile.PreferredCategories.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (preferredCategories.Any(category =>
                            job.Description.Contains(category, StringComparison.OrdinalIgnoreCase) ||
                            job.Title.Contains(category, StringComparison.OrdinalIgnoreCase)))
                        {
                            await _notificationService.CreateNotificationAsync(
                                jobSeeker.Id,
                                $"Có công việc mới phù hợp với sở thích của bạn: '{job.Title}' tại {job.Company}."
                            );
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (job.User != null && !string.IsNullOrEmpty(job.User.Email))
                {
                    var subject = "Công việc đã được duyệt";
                    var body = $"<p>Công việc <strong>{job.Title}</strong> đã được duyệt và hiển thị công khai.</p>";
                    await _emailSender.SendEmailAsync(job.User.Email, subject, body);
                }

                _logger.LogInformation("Đã duyệt công việc ID {JobId}.", id);
                TempData["SuccessMessage"] = "Duyệt công việc thành công!";
                return RedirectToAction("Jobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi duyệt công việc ID {JobId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi duyệt công việc.";
                return RedirectToAction("Jobs");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectJob(int id, string reason)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.User)
                    .FirstOrDefaultAsync(j => j.Id == id);
                if (job == null) return NotFound("Công việc không tồn tại.");

                job.Status = "Bị từ chối";
                await _notificationService.CreateNotificationAsync(
                    job.UserId,
                    $"Công việc '{job.Title}' bị từ chối. Lý do: {reason}"
                );
                await _context.SaveChangesAsync();

                if (job.User != null && !string.IsNullOrEmpty(job.User.Email))
                {
                    var subject = "Công việc bị từ chối";
                    var body = $"<p>Công việc <strong>{job.Title}</strong> đã bị từ chối. Lý do: {reason}</p>";
                    await _emailSender.SendEmailAsync(job.User.Email, subject, body);
                }

                _logger.LogInformation("Đã từ chối công việc ID {JobId} với lý do: {Reason}.", id, reason);
                TempData["SuccessMessage"] = "Từ chối công việc thành công!";
                return RedirectToAction("Jobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi từ chối công việc ID {JobId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi từ chối công việc.";
                return RedirectToAction("Jobs");
            }
        }

        [Route("Admin/ApplicationDetails")]
        public IActionResult RedirectToApplicationsIfNoId()
        {
            TempData["ErrorMessage"] = "Vui lòng chọn một đơn ứng tuyển để xem chi tiết.";
            return RedirectToAction("Applications");
        }

        public async Task<IActionResult> ApplicationDetails(int id)
        {
            try
            {
                var application = await _context.JobApplications
                    .Include(a => a.Job)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn ứng tuyển.";
                    return NotFound();
                }

                _logger.LogInformation("Đã tải chi tiết đơn ứng tuyển ID {ApplicationId}.", id);
                return View(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết đơn ứng tuyển ID {ApplicationId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết đơn ứng tuyển.";
                return RedirectToAction("Applications");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus(int id, string status)
        {
            try
            {
                var application = await _context.JobApplications
                    .Include(a => a.Job)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null) return NotFound("Đơn ứng tuyển không tồn tại.");

                application.Status = status;
                await _context.SaveChangesAsync();

                // Thêm thông báo cho ứng viên
                await _notificationService.CreateNotificationAsync(
                    application.UserId,
                    $"Trạng thái đơn ứng tuyển của bạn cho công việc '{application.Job.Title}' đã được cập nhật thành: {status}"
                );

                // Gửi email thông báo cho ứng viên
                if (application.User != null && !string.IsNullOrEmpty(application.User.Email))
                {
                    var subject = $"Cập nhật trạng thái đơn ứng tuyển - {application.Job.Title}";
                    var body = $"<p>Xin chào {application.User.UserName},</p>" +
                               $"<p>Trạng thái đơn ứng tuyển của bạn cho công việc <strong>{application.Job.Title}</strong> " +
                               $"tại công ty <strong>{application.Job.Company}</strong> đã được cập nhật thành: <strong>{status}</strong>.</p>" +
                               $"<p>Trân trọng,<br/>Đội ngũ JobSearchWebsite</p>";
                    await _emailSender.SendEmailAsync(application.User.Email, subject, body);
                    _logger.LogInformation("Đã gửi email thông báo tới {Email} về trạng thái đơn ứng tuyển ID {ApplicationId}.", application.User.Email, id);
                }
                else
                {
                    _logger.LogWarning("Không thể gửi email cho đơn ứng tuyển ID {ApplicationId}: Email người dùng không tồn tại.", id);
                }

                _logger.LogInformation("Đã cập nhật trạng thái đơn ứng tuyển ID {ApplicationId} thành {Status}.", id, status);
                TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn ứng tuyển thành {status} thành công!";
                return RedirectToAction("ApplicationDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn ứng tuyển ID {ApplicationId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái đơn ứng tuyển.";
                return RedirectToAction("ApplicationDetails", new { id });
            }
        }

        // Action mới: Xem tất cả thông báo
        public async Task<IActionResult> AllNotifications(int page = 1, int pageSize = 10)
        {
            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync(page, pageSize);
                var totalItems = await _context.Notifications.CountAsync();
                var paginatedNotifications = notifications
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(); // Bỏ Include vì đã xử lý trong NotificationService

                ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                ViewBag.CurrentPage = page;
                return View(paginatedNotifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách thông báo.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách thông báo.";
                return RedirectToAction("Index");
            }
        }

        // Action mới: Thống kê thông báo
        public async Task<IActionResult> NotificationStatistics(int? year, int? month)
        {
            try
            {
                var notifications = _context.Notifications.AsQueryable();

                if (year.HasValue)
                    notifications = notifications.Where(n => n.CreatedDate.Year == year.Value);
                if (month.HasValue)
                    notifications = notifications.Where(n => n.CreatedDate.Month == month.Value);

                ViewBag.TotalNotifications = await notifications.CountAsync();
                ViewBag.UnreadNotifications = await notifications.CountAsync(n => !n.IsRead);
                ViewBag.UnreadAdminNotifications = await notifications
                    .CountAsync(n => n.UserId == _userManager.GetUserId(User) && !n.IsRead);
                ViewBag.UnreadEmployerNotifications = await notifications
                    .Join(_userManager.Users.Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur }),
                          n => n.UserId, u => u.u.Id, (n, u) => new { n, u })
                    .Join(_context.Roles, nu => nu.u.ur.RoleId, r => r.Id, (nu, r) => new { nu.n, r })
                    .Where(x => x.r.Name == "Employer" && !x.n.IsRead)
                    .CountAsync();
                ViewBag.UnreadJobSeekerNotifications = await notifications
                    .Join(_userManager.Users.Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur }),
                          n => n.UserId, u => u.u.Id, (n, u) => new { n, u })
                    .Join(_context.Roles, nu => nu.u.ur.RoleId, r => r.Id, (nu, r) => new { nu.n, r })
                    .Where(x => x.r.Name == "JobSeeker" && !x.n.IsRead)
                    .CountAsync();

                ViewBag.SelectedYear = year;
                ViewBag.SelectedMonth = month;

                _logger.LogInformation("Đã tạo thống kê thông báo cho năm {Year} và tháng {Month}.", year, month);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thống kê thông báo cho năm {Year} và tháng {Month}.", year, month);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo thống kê thông báo.";
                return RedirectToAction("Index");
            }
        }

        // Action mới: Quản lý thông báo (đánh dấu đã đọc/xóa)
        [HttpPost]
        public async Task<IActionResult> ManageNotifications(int id, string action)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null) return NotFound("Thông báo không tồn tại.");

                if (action == "MarkAsRead")
                {
                    notification.IsRead = true;
                    await _notificationService.MarkAsReadAsync(notification);
                    _logger.LogInformation("Đã đánh dấu thông báo ID {NotificationId} là đã đọc.", id);
                }
                else if (action == "Delete")
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã xóa thông báo ID {NotificationId}.", id);
                }

                TempData["SuccessMessage"] = "Quản lý thông báo thành công!";
                return RedirectToAction("AllNotifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quản lý thông báo ID {NotificationId}.", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi quản lý thông báo.";
                return RedirectToAction("AllNotifications");
            }
        }
    }
}
